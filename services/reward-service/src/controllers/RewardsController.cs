using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RewardService.Dtos;
using RewardService.Services;

namespace RewardService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RewardsController(IRewardService rewardService) : ControllerBase
{
    /// <summary>
    /// POST /api/rewards/assign
    /// Assigns points to the authenticated user based on a trigger event.
    ///
    /// Example body:
    /// { "triggerEvent": "purchase", "purchaseAmount": 60.00, "idempotencyKey": "order-abc123" }
    /// { "triggerEvent": "referral" }
    /// { "triggerEvent": "daily_login" }
    /// </summary>
    [HttpPost("assign")]
    public async Task<IActionResult> AssignPoints([FromBody] AssignPointsRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, error, result) = await rewardService.AssignPointsAsync(userId.Value, request);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    /// <summary>
    /// POST /api/rewards/redeem
    /// Redeems points for wallet credit. Minimum 100 points = $1.00.
    ///
    /// Example body: { "pointsToRedeem": 500 }  →  $5.00 credited to wallet
    /// </summary>
    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemPoints([FromBody] RedeemPointsRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, error, result) = await rewardService.RedeemPointsAsync(userId.Value, request);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(result);
    }

    /// <summary>GET /api/rewards/transactions — view transaction history</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var history = await rewardService.GetTransactionHistoryAsync(userId.Value);
        return Ok(history);
    }

    /// <summary>GET /api/rewards/criteria — view all active earning rules</summary>
    [AllowAnonymous]
    [HttpGet("criteria")]
    public async Task<IActionResult> GetCriteria()
    {
        var criteria = await rewardService.GetActiveCriteriaAsync();
        return Ok(criteria);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : null;
    }
}
