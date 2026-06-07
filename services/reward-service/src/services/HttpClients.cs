using System.Text;
using System.Text.Json;

namespace RewardService.HttpClients;

/// <summary>
/// CONCEPT: Typed HttpClient — a strongly-typed wrapper around HttpClient.
/// Registered in DI as: builder.Services.AddHttpClient&lt;UserServiceClient&gt;(...)
/// The HttpClient is managed by IHttpClientFactory (connection pooling, DNS refresh).
/// </summary>
public class UserServiceClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>Updates user's point balance by calling user-service directly (HTTP)</summary>
    public async Task<bool> UpdatePointsAsync(Guid userId, int pointsDelta, string operation)
    {
        var body = JsonSerializer.Serialize(new { pointsDelta, operation });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PatchAsync($"/api/users/{userId}/points", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<int?> GetPointBalanceAsync(Guid userId)
    {
        var response = await http.GetAsync($"/api/users/{userId}/balance");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json, Json);
        return data.GetProperty("balance").GetInt32();
    }
}

public class WalletServiceClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Credits money to the user's virtual wallet.
    /// CONCEPT: We pass an idempotency key so wallet-service can safely
    /// reject duplicate calls (e.g. if reward-service retries on failure).
    /// </summary>
    public async Task<bool> CreditWalletAsync(Guid userId, decimal amount, string idempotencyKey)
    {
        var body = JsonSerializer.Serialize(new { userId, amount, idempotencyKey });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync("/api/wallets/credit", content);
        return response.IsSuccessStatusCode;
    }
}
