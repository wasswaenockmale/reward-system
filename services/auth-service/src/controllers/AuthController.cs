using AuthService.Dtos;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

/// <summary>
/// CONCEPT: Controllers handle HTTP requests and return HTTP responses.
/// [ApiController] enables automatic model validation and problem details.
/// [Route] defines the URL path prefix for all actions in this controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// POST /api/auth/register
    /// Creates a new user account and returns a JWT token.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // CONCEPT: Basic validation — in production use FluentValidation
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters" });

        var (success, error, response) = await authService.RegisterAsync(request);

        if (!success)
            return BadRequest(new { message = error });

        // 201 Created with the auth response
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await authService.LoginAsync(request);

        if (!success)
            return Unauthorized(new { message = error });

        return Ok(response);
    }

    /// <summary>
    /// GET /api/auth/me
    /// Returns the current user's info from the JWT claims (no DB call needed).
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        // CONCEPT: User is a ClaimsPrincipal — populated automatically from the JWT
        // by the authentication middleware. We read claims from it.
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                 ?? User.FindFirst("email")?.Value;

        return Ok(new { userId, email });
    }
}
