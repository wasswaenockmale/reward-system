using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

/// <summary>
/// CONCEPT: DbContext is the bridge between C# and the database.
/// It represents a session with the database — you query through it, save through it.
///
/// Each DbSet&lt;T&gt; = one database table.
/// EF Core generates the SQL for you based on LINQ queries.
/// </summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // CONCEPT: Fluent API configuration — defines constraints without attributes.
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            // Unique index — no two users can have the same email
            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();
        });
    }
}
