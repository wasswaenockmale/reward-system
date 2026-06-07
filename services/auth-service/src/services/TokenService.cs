using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

/// <summary>
/// CONCEPT: Interface — defines a contract without an implementation.
/// Any class can implement ITokenService. This allows unit tests to use a
/// fake/mock token service without needing a real secret key.
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user);
}

/// <summary>
/// CONCEPT: The concrete implementation. Registered in DI as:
///   builder.Services.AddScoped&lt;ITokenService, TokenService&gt;()
///
/// IConfiguration is injected automatically — it reads from appsettings.json
/// and environment variables (env vars override appsettings in Docker).
/// </summary>
public class TokenService(IConfiguration config) : ITokenService
{
    public string GenerateToken(User user)
    {
        var secret = config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // CONCEPT: Claims are key-value pairs embedded in the JWT.
        // Other services can read these without a database call.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // unique token ID
        };

        var expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
