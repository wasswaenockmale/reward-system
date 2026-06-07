using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Dtos;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserProfileService userService) : ControllerBase
{
    /// <summary>GET /api/users/me — get current user's profile</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null) return Unauthorized();

        var profile = await userService.GetByAuthUserIdAsync(authUserId.Value);
        if (profile is null) return NotFound(new { message = "Profile not found" });

        return Ok(profile);
    }

    /// <summary>GET /api/users/me/balance — get current user's point balance</summary>
    [Authorize]
    [HttpGet("me/balance")]
    public async Task<IActionResult> GetBalance()
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null) return Unauthorized();

        var balance = await userService.GetBalanceAsync(authUserId.Value);
        if (balance is null) return NotFound();

        return Ok(balance);
    }

    /// <summary>
    /// POST /api/users — internal endpoint called by auth-service after registration.
    /// In production, protect this with an internal API key or network policy.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserProfileRequest request)
    {
        var profile = await userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetMe), profile);
    }

    /// <summary>
    /// PATCH /api/users/{authUserId}/points — internal endpoint called by reward-service.
    /// Updates point balance when points are assigned or redeemed.
    /// </summary>
    [HttpPatch("{authUserId:guid}/points")]
    public async Task<IActionResult> UpdatePoints(
        Guid authUserId,
        [FromBody] UpdatePointsRequest request)
    {
        var success = await userService.UpdatePointsAsync(authUserId, request);
        if (!success)
            return BadRequest(new { message = "Insufficient points or user not found" });

        return NoContent();
    }

    // CONCEPT: Helper method to extract the user ID from the JWT claims.
    // The JWT middleware already validated the token — we just read the claims.
    private Guid? GetAuthUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : null;
    }
}
