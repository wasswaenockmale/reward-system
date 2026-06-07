using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Dtos;
using UserService.Models;

namespace UserService.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetByAuthUserIdAsync(Guid authUserId);
    Task<UserProfileResponse> CreateAsync(CreateUserProfileRequest request);
    Task<PointBalanceResponse?> GetBalanceAsync(Guid authUserId);
    Task<bool> UpdatePointsAsync(Guid authUserId, UpdatePointsRequest request);
}

public class UserProfileService(UserDbContext db) : IUserProfileService
{
    public async Task<UserProfileResponse?> GetByAuthUserIdAsync(Guid authUserId)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.AuthUserId == authUserId);

        return profile is null ? null : MapToResponse(profile);
    }

    public async Task<UserProfileResponse> CreateAsync(CreateUserProfileRequest request)
    {
        var profile = new UserProfile
        {
            AuthUserId = request.AuthUserId,
            Email = request.Email.ToLower(),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        return MapToResponse(profile);
    }

    public async Task<PointBalanceResponse?> GetBalanceAsync(Guid authUserId)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.AuthUserId == authUserId);

        if (profile is null) return null;

        return new PointBalanceResponse(
            profile.AuthUserId,
            profile.PointBalance,
            profile.TotalPointsEarned,
            profile.TotalPointsRedeemed);
    }

    /// <summary>
    /// CONCEPT: Optimistic concurrency — we update the balance atomically.
    /// In a production system, you'd use a database transaction or row-level locking
    /// to prevent race conditions (two requests updating the balance simultaneously).
    /// </summary>
    public async Task<bool> UpdatePointsAsync(Guid authUserId, UpdatePointsRequest request)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(u => u.AuthUserId == authUserId);

        if (profile is null) return false;

        // Prevent balance going negative
        if (profile.PointBalance + request.PointsDelta < 0)
            return false;

        profile.PointBalance += request.PointsDelta;
        profile.UpdatedAt = DateTime.UtcNow;

        if (request.PointsDelta > 0)
            profile.TotalPointsEarned += request.PointsDelta;
        else
            profile.TotalPointsRedeemed += Math.Abs(request.PointsDelta);

        await db.SaveChangesAsync();
        return true;
    }

    private static UserProfileResponse MapToResponse(UserProfile p) =>
        new(p.Id, p.AuthUserId, p.Email, p.FirstName, p.LastName,
            p.PointBalance, p.TotalPointsEarned, p.TotalPointsRedeemed, p.CreatedAt);
}
