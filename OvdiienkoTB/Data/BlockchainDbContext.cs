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
            .ToTable("Users");
        
        modelBuilder.Entity<Wallet>()
            .ToTable("Wallets")
            .Property(w => w.Id)
            .ValueGeneratedOnAdd();

        base.OnModelCreating(modelBuilder);
    }
}