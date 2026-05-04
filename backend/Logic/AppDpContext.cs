using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>()
        .HasMany(c => c.Items)
        .WithOne(i => i.Cart)
        .HasForeignKey(i => i.CartId);
        
        modelBuilder.Entity<Orders>()
            .HasKey(o => o.OrderId);

        modelBuilder.Entity<Orders>()
            .HasOne(o => o.OrderAddress)
            .WithOne(a => a.Order)
            .HasForeignKey<OrderAddress>(a => a.OrderId);
    }
}