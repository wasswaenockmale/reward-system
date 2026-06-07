using Microsoft.EntityFrameworkCore;
using WalletService.Models;

namespace WalletService.Data;

public class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId).IsUnique();
            e.Property(w => w.Balance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.IdempotencyKey).IsUnique();
            e.Property(t => t.Amount).HasPrecision(18, 2);
        });
    }
}
