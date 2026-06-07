namespace WalletService.Models;

/// <summary>
/// A user's virtual wallet — holds money credited from point redemptions.
/// </summary>
public class Wallet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Balance { get; set; } = 0;
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// CONCEPT: Immutable ledger of every credit/debit.
/// Idempotency key ensures reward-service can retry without double-crediting.
/// </summary>
public class WalletTransaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }        // positive = credit, negative = debit
    public string Description { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
