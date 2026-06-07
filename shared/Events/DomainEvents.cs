namespace Shared.Events;

/// <summary>
/// Published by reward-service when points are assigned to a user.
/// Consumed by: notification-service, user-service (to update balance cache).
/// </summary>
public record PointsAssignedEvent(
    Guid UserId,
    int Points,
    string Reason,
    Guid TransactionId,
    DateTime OccurredAt);

/// <summary>
/// Published by reward-service when a user redeems points.
/// Consumed by: notification-service.
/// </summary>
public record PointsRedeemedEvent(
    Guid UserId,
    int PointsRedeemed,
    decimal MoneyAmount,
    Guid TransactionId,
    DateTime OccurredAt);

/// <summary>
/// Published by wallet-service after crediting money.
/// Consumed by: notification-service.
/// </summary>
public record WalletCreditedEvent(
    Guid UserId,
    Guid WalletId,
    decimal AmountCredited,
    decimal NewBalance,
    DateTime OccurredAt);
