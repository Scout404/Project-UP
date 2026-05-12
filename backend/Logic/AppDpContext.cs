using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
    public DbSet<Size> Sizes { get; set; } = null!;
    public DbSet<Color> Colors { get; set; } = null!;
    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Orders> Orders { get; set; } = null!;
    public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
    public DbSet<OrderAddress> OrderAddresses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(v => v.ProductVariantId);

            entity.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Size)
                .WithMany()
                .HasForeignKey(v => v.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Color)
                .WithMany()
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(v => v.PictureUrl).IsRequired();
        });

        modelBuilder.Entity<Orders>()
            .HasKey(o => o.OrderId);

        modelBuilder.Entity<OrderAddress>()
            .HasKey(a => a.OrderId);

        modelBuilder.Entity<Orders>()
            .HasOne(o => o.OrderAddress)
            .WithOne(a => a.Order)
            .HasForeignKey<OrderAddress>(a => a.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasMany(p => p.Reviews)
            .WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cart>()
            .HasMany(c => c.Items)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .Property(p => p.BasePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CartItem>()
            .Property(ci => ci.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        modelBuilder.Entity<ProductVariant>()
            .HasIndex(v => v.ProductId);

        modelBuilder.Entity<Cart>()
            .HasIndex(c => c.UserId);

        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => ci.CartId);

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.OrderId);

        modelBuilder.Entity<WishlistItem>()
            .HasIndex(w => w.ProductVariantId);
    }
}