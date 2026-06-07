using MassTransit;
using Microsoft.EntityFrameworkCore;
using WalletService.Data;
using WalletService.Models;
using Shared.Events;

namespace WalletService.Services;

public record CreditRequest(Guid UserId, decimal Amount, string IdempotencyKey);
public record WalletResponse(Guid WalletId, Guid UserId, decimal Balance, string Currency);

public interface IWalletService
{
    Task<WalletResponse> GetOrCreateWalletAsync(Guid userId);
    Task<(bool Success, string? Error, WalletResponse? Wallet)> CreditAsync(CreditRequest request);
}

public class WalletServiceImpl(WalletDbContext db, IPublishEndpoint bus) : IWalletService
{
    public async Task<WalletResponse> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet is not null) return Map(wallet);

        wallet = new Wallet { UserId = userId };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return Map(wallet);
    }

    /// <summary>
    /// CONCEPT: Idempotent credit — if the same idempotencyKey is sent twice,
    /// we return success without double-crediting. This is essential when
    /// reward-service retries after a network timeout.
    /// </summary>
    public async Task<(bool Success, string? Error, WalletResponse? Wallet)> CreditAsync(
        CreditRequest request)
    {
        // Check if this exact credit was already processed
        var alreadyProcessed = await db.WalletTransactions
            .AnyAsync(t => t.IdempotencyKey == request.IdempotencyKey);

        if (alreadyProcessed)
            return (true, null, await GetOrCreateWalletAsync(request.UserId)); // idempotent: success

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId);
        if (wallet is null)
        {
            // Auto-create wallet if it doesn't exist
            wallet = new Wallet { UserId = request.UserId };
            db.Wallets.Add(wallet);
        }

        wallet.Balance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var tx = new WalletTransaction
        {
            WalletId = wallet.Id,
            UserId = request.UserId,
            Amount = request.Amount,
            Description = $"Point redemption credit: ${request.Amount:F2}",
            IdempotencyKey = request.IdempotencyKey
        };
        db.WalletTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Publish event so notification-service can notify the user
        await bus.Publish(new WalletCreditedEvent(
            request.UserId, wallet.Id, request.Amount, wallet.Balance, DateTime.UtcNow));

        return (true, null, Map(wallet));
    }

    private static WalletResponse Map(Wallet w) =>
        new(w.Id, w.UserId, w.Balance, w.Currency);
}
