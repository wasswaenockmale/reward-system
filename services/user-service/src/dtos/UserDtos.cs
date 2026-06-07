namespace UserService.Dtos;

public record CreateUserProfileRequest(
    Guid AuthUserId,
    string Email,
    string FirstName,
    string LastName);

public record UserProfileResponse(
    Guid Id,
    Guid AuthUserId,
    string Email,
    string FirstName,
    string LastName,
    int PointBalance,
    int TotalPointsEarned,
    int TotalPointsRedeemed,
    DateTime CreatedAt);

public record PointBalanceResponse(
    Guid UserId,
    int Balance,
    int TotalEarned,
    int TotalRedeemed);

public record UpdatePointsRequest(
    int PointsDelta,       // positive = add, negative = deduct
    string Operation);     // "earn" | "redeem"
