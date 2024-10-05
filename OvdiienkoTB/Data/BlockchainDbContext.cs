using Microsoft.EntityFrameworkCore;
using OvdiienkoTB.Models;

namespace OvdiienkoTB.Data;

public class BlockchainDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    //public DbSet<Block> Blocks { get; set; }
    
    public BlockchainDbContext(DbContextOptions<BlockchainDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users").HasMany(u => u.Wallets).WithOne(w => w.User);
        modelBuilder.Entity<Wallet>().ToTable("Users").HasOne(w => w.User).WithMany(u => u.Wallets);
        //modelBuilder.Entity<Block>().ToTable("Users");
        base.OnModelCreating(modelBuilder);
    }
}