namespace AuthService.Dtos;

/// <summary>
/// CONCEPT: DTOs (Data Transfer Objects) are what the API receives and returns.
/// They are NOT the database model — they're the "public face" of the service.
/// This protects your database schema from being exposed directly.
/// </summary>

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);

public record LoginRequest(
    string Email,
    string Password);

public record AuthResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,   // seconds
    Guid UserId,
    string Email);

public record ValidationErrorResponse(
    string Message,
    IEnumerable<string> Errors);
