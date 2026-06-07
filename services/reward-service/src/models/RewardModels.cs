namespace RewardService.Models;

/// <summary>
/// Records every point event — earns and redemptions.
/// CONCEPT: Append-only log. We never delete or update transactions.
/// The current balance is derived from summing all transactions.
/// (user-service caches the current balance for fast reads)
/// </summary>
public class PointTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }       // auth-service user ID
    public int Points { get; set; }        // positive = earned, negative = redeemed
    public TransactionType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Idempotency key — prevents duplicate transactions</summary>
    public string? IdempotencyKey { get; set; }
}

public enum TransactionType
{
    Earned,
    Redeemed
}

/// <summary>
/// Defines the rules for earning points.
/// CONCEPT: Instead of hardcoding "1 purchase = 10 points", we store criteria
/// in the database so they can be changed without redeploying.
/// </summary>
public class RewardCriteria
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;   // e.g. "purchase", "referral", "daily_login"
    public int PointsPerUnit { get; set; }                     // e.g. 10 points per $1
    public decimal? MinimumAmount { get; set; }                // optional minimum (e.g. min $10 purchase)
    public int? BonusPoints { get; set; }                      // flat bonus (e.g. 500 for referral)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks redemptions — each redemption converts points to wallet credit.
/// </summary>
public class PointRedemption
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int PointsRedeemed { get; set; }
    public decimal MoneyAmount { get; set; }       // PointsRedeemed / PointsPerDollar
    public RedemptionStatus Status { get; set; } = RedemptionStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? IdempotencyKey { get; set; }
}

public enum RedemptionStatus
{
    Pending,
    Completed,
    Failed
}
