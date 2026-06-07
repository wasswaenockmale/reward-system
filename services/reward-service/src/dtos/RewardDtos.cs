namespace RewardService.Dtos;

public record AssignPointsRequest(
    string TriggerEvent,       // "purchase" | "referral" | "daily_login"
    decimal? PurchaseAmount,   // required for "purchase" event
    string? IdempotencyKey);   // optional — prevents duplicate assignments

public record AssignPointsResponse(
    Guid TransactionId,
    int PointsAwarded,
    string Reason);

public record RedeemPointsRequest(
    int PointsToRedeem,
    string? IdempotencyKey);

public record RedeemPointsResponse(
    Guid RedemptionId,
    int PointsRedeemed,
    decimal MoneyAmount,
    string Status);

public record TransactionHistoryItem(
    Guid Id,
    int Points,
    string Type,
    string Reason,
    DateTime CreatedAt);

public record CriteriaResponse(
    Guid Id,
    string Name,
    string TriggerEvent,
    int PointsPerUnit,
    decimal? MinimumAmount,
    int? BonusPoints,
    bool IsActive);
