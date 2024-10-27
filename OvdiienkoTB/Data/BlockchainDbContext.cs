using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Data;

public class BlockchainDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    
    public BlockchainDbContext(DbContextOptions<BlockchainDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .ToTable("Users")
            .HasMany(u => u.Wallets)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.Id);
        
        modelBuilder.Entity<Wallet>()
            .ToTable("Wallets")
            .HasOne(w => w.User)
            .WithMany(u => u.Wallets)
            .HasForeignKey(w => w.Id);

        base.OnModelCreating(modelBuilder);
    }
}