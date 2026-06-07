using AuthService.Data;
using AuthService.Dtos;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
}

/// <summary>
/// CONCEPT: async/await — all database operations are asynchronous.
/// "await" suspends this method until the database responds, freeing the thread
/// to handle other requests. Without this, one slow DB query would block everything.
///
/// Task&lt;T&gt; = a promise that will eventually return T.
/// </summary>
public class AuthService(AuthDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(
        RegisterRequest request)
    {
        // Check if email already exists
        var exists = await db.Users.AnyAsync(u => u.Email == request.Email.ToLower());
        if (exists)
            return (false, "Email already registered", null);

        // CONCEPT: BCrypt hashes the password. The hash includes a random salt,
        // so the same password hashes differently each time. Work factor 11 = slow
        // enough to resist brute-force attacks.
        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(); // Executes INSERT INTO users ...

        var token = tokenService.GenerateToken(user);
        return (true, null, BuildResponse(user, token));
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(
        LoginRequest request)
    {
        // CONCEPT: FirstOrDefaultAsync returns null if not found — no exception.
        // SingleOrDefaultAsync would throw if multiple rows match.
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);

        if (user is null)
            return (false, "Invalid email or password", null);

        // BCrypt.Verify compares the plain password against the stored hash
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return (false, "Invalid email or password", null);

        var token = tokenService.GenerateToken(user);
        return (true, null, BuildResponse(user, token));
    }

    private static AuthResponse BuildResponse(User user, string token) =>
        new(token, "Bearer", 3600, user.Id, user.Email);
}
