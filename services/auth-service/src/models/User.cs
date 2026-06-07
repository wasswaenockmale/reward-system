namespace AuthService.Models;

/// <summary>
/// CONCEPT: This is an Entity — a C# class that EF Core maps to a database table.
/// Each property becomes a column. The "Id" property is automatically the primary key.
/// </summary>
public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// NEVER store plain passwords. BCrypt hashes them with a salt.
    /// "password123" → "$2a$11$xyz..." (irreversible)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
