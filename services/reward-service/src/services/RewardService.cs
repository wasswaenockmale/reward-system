using MassTransit;
using Microsoft.EntityFrameworkCore;
using RewardService.Data;
using RewardService.Dtos;
using RewardService.HttpClients;
using RewardService.Models;
using Shared.Events;

namespace RewardService.Services;

public interface IRewardService
{
    Task<(bool Success, string? Error, AssignPointsResponse? Result)> AssignPointsAsync(
        Guid userId, AssignPointsRequest request);

    Task<(bool Success, string? Error, RedeemPointsResponse? Result)> RedeemPointsAsync(
        Guid userId, RedeemPointsRequest request);

    Task<IEnumerable<TransactionHistoryItem>> GetTransactionHistoryAsync(Guid userId);
    Task<IEnumerable<CriteriaResponse>> GetActiveCriteriaAsync();
}

/// <summary>
/// CONCEPT: This is the heart of the system — the domain service.
/// It coordinates:
///   1. Database (RewardDbContext) — for transactions and criteria
///   2. UserServiceClient (HTTP) — to update the user's balance
///   3. WalletServiceClient (HTTP) — to credit money on redemption
///   4. IPublishEndpoint (MassTransit) — to publish events to RabbitMQ
///
/// All dependencies are injected via the constructor (constructor injection pattern).
/// </summary>
public class RewardServiceImpl(
    RewardDbContext db,
    UserServiceClient userClient,
    WalletServiceClient walletClient,
    IPublishEndpoint bus) : IRewardService
{
    // CONCEPT: How many points equal $1 in wallet credit
    private const int PointsPerDollar = 100;

    public async Task<(bool Success, string? Error, AssignPointsResponse? Result)> AssignPointsAsync(
        Guid userId, AssignPointsRequest request)
    {
        // Idempotency check: skip if this exact request was already processed
        if (request.IdempotencyKey is not null)
        {
            var exists = await db.Transactions
                .AnyAsync(t => t.IdempotencyKey == request.IdempotencyKey);
            if (exists)
                return (false, "Duplicate request — already processed", null);
        }

        // Find the matching active criteria
        var criteria = await db.Criteria
            .FirstOrDefaultAsync(c => c.TriggerEvent == request.TriggerEvent && c.IsActive);

        if (criteria is null)
            return (false, $"No active criteria found for event: {request.TriggerEvent}", null);

        // ── Calculate points based on criteria ───────────────────────────────
        int pointsToAward;
        string reason;

        if (request.TriggerEvent == "purchase")
        {
            if (request.PurchaseAmount is null || request.PurchaseAmount < (criteria.MinimumAmount ?? 0))
                return (false, $"Minimum purchase amount is ${criteria.MinimumAmount}", null);

            // e.g. $60 purchase × 10 points/$1 = 600 points
            pointsToAward = (int)(request.PurchaseAmount.Value * criteria.PointsPerUnit);
            reason = $"Purchase of ${request.PurchaseAmount:F2} ({criteria.PointsPerUnit} pts/$1)";
        }
        else
        {
            // Flat bonus events (referral, daily_login, etc.)
            pointsToAward = criteria.BonusPoints ?? 0;
            reason = criteria.Name;
        }

        if (pointsToAward <= 0)
            return (false, "No points to award", null);

        // ── Save transaction ──────────────────────────────────────────────────
        var transaction = new PointTransaction
        {
            UserId = userId,
            Points = pointsToAward,
            Type = TransactionType.Earned,
            Reason = reason,
            IdempotencyKey = request.IdempotencyKey
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // ── Update user balance (HTTP call to user-service) ───────────────────
        await userClient.UpdatePointsAsync(userId, pointsToAward, "earn");

        // ── Publish event to RabbitMQ (async — notification-service listens) ──
        // CONCEPT: Fire-and-forget. If notification-service is down, RabbitMQ
        // holds the message and delivers it when the service comes back up.
        await bus.Publish(new PointsAssignedEvent(
            userId, pointsToAward, reason, transaction.Id, DateTime.UtcNow));

        return (true, null, new AssignPointsResponse(transaction.Id, pointsToAward, reason));
    }

    public async Task<(bool Success, string? Error, RedeemPointsResponse? Result)> RedeemPointsAsync(
        Guid userId, RedeemPointsRequest request)
    {
        if (request.PointsToRedeem <= 0)
            return (false, "Points to redeem must be greater than zero", null);

        // Minimum redemption: 100 points = $1
        if (request.PointsToRedeem < PointsPerDollar)
            return (false, $"Minimum redemption is {PointsPerDollar} points", null);

        // Idempotency check
        if (request.IdempotencyKey is not null)
        {
            var exists = await db.Redemptions
                .AnyAsync(r => r.IdempotencyKey == request.IdempotencyKey);
            if (exists)
                return (false, "Duplicate request — already processed", null);
        }

        // Check user has enough points
        var currentBalance = await userClient.GetPointBalanceAsync(userId);
        if (currentBalance is null || currentBalance < request.PointsToRedeem)
            return (false, "Insufficient points", null);

        var moneyAmount = (decimal)request.PointsToRedeem / PointsPerDollar;
        var idempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString();

        // Save the redemption record as Pending
        var redemption = new PointRedemption
        {
            UserId = userId,
            PointsRedeemed = request.PointsToRedeem,
            MoneyAmount = moneyAmount,
            Status = RedemptionStatus.Pending,
            IdempotencyKey = idempotencyKey
        };
        db.Redemptions.Add(redemption);

        // Save transaction (deduction)
        var transaction = new PointTransaction
        {
            UserId = userId,
            Points = -request.PointsToRedeem,
            Type = TransactionType.Redeemed,
            Reason = $"Redeemed {request.PointsToRedeem} points for ${moneyAmount:F2}",
            IdempotencyKey = idempotencyKey + "_tx"
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        // Deduct points from user-service
        await userClient.UpdatePointsAsync(userId, -request.PointsToRedeem, "redeem");

        // Credit wallet (HTTP call to wallet-service)
        var walletCredited = await walletClient.CreditWalletAsync(userId, moneyAmount, idempotencyKey);

        // Update redemption status
        redemption.Status = walletCredited ? RedemptionStatus.Completed : RedemptionStatus.Failed;
        await db.SaveChangesAsync();

        if (!walletCredited)
            return (false, "Failed to credit wallet. Points have been deducted — contact support.", null);

        // Publish event
        await bus.Publish(new PointsRedeemedEvent(
            userId, request.PointsToRedeem, moneyAmount, redemption.Id, DateTime.UtcNow));

        return (true, null, new RedeemPointsResponse(
            redemption.Id, request.PointsToRedeem, moneyAmount, "Completed"));
    }

    public async Task<IEnumerable<TransactionHistoryItem>> GetTransactionHistoryAsync(Guid userId)
    {
        // CONCEPT: LINQ query — compiles to SQL at runtime via EF Core
        return await db.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new TransactionHistoryItem(
                t.Id, t.Points, t.Type.ToString(), t.Reason, t.CreatedAt))
            .ToListAsync();
    }

    public async Task<IEnumerable<CriteriaResponse>> GetActiveCriteriaAsync()
    {
        return await db.Criteria
            .Where(c => c.IsActive)
            .Select(c => new CriteriaResponse(
                c.Id, c.Name, c.TriggerEvent, c.PointsPerUnit,
                c.MinimumAmount, c.BonusPoints, c.IsActive))
            .ToListAsync();
    }
}
