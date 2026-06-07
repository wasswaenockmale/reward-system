namespace UserService.Models;

/// <summary>
/// Stores user profile information and their current point balance.
/// CONCEPT: user-service owns its own copy of user data — independent of auth-service.
/// The UserId here corresponds to the Id from auth-service (same Guid, shared as a key).
/// </summary>
public class UserProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// This is the same Guid as the User.Id in auth-service.
    /// Services share IDs, not database tables.
    /// </summary>
    public Guid AuthUserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Current point balance = TotalEarned - TotalRedeemed.
    /// Stored redundantly for fast reads (no need to sum transactions).
    /// </summary>
    public int PointBalance { get; set; } = 0;

    public int TotalPointsEarned { get; set; } = 0;
    public int TotalPointsRedeemed { get; set; } = 0;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
