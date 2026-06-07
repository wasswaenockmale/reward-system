using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletService.Services;

namespace WalletService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletsController(IWalletService walletService) : ControllerBase
{
    /// <summary>GET /api/wallets/me — get current user's wallet balance</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var wallet = await walletService.GetOrCreateWalletAsync(userId.Value);
        return Ok(wallet);
    }

    /// <summary>
    /// POST /api/wallets/credit — INTERNAL: called by reward-service only.
    /// Credits money to a user's virtual wallet.
    ///
    /// Body: { "userId": "...", "amount": 5.00, "idempotencyKey": "redemption-xyz" }
    /// </summary>
    [HttpPost("credit")]
    public async Task<IActionResult> Credit([FromBody] CreditRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new { message = "Amount must be positive" });

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { message = "IdempotencyKey is required" });

        var (success, error, wallet) = await walletService.CreditAsync(request);

        if (!success)
            return StatusCode(500, new { message = error });

        return Ok(wallet);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : null;
    }
}
