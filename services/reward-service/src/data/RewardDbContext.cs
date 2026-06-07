using Microsoft.EntityFrameworkCore;
using RewardService.Models;

namespace RewardService.Data;

public class RewardDbContext(DbContextOptions<RewardDbContext> options) : DbContext(options)
{
    public DbSet<PointTransaction> Transactions { get; set; }
    public DbSet<RewardCriteria> Criteria { get; set; }
    public DbSet<PointRedemption> Redemptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PointTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.UserId);
            e.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.Property(t => t.Type).HasConversion<string>();
        });

        modelBuilder.Entity<RewardCriteria>(e =>
        {
            e.HasKey(c => c.Id);
        });

        modelBuilder.Entity<PointRedemption>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.Property(r => r.Status).HasConversion<string>();
        });

        // NOTE: Seed data is in the InitialCreate migration (20240101000000_InitialCreate.cs)
        // so it is applied automatically when the database is created.
    }
}
