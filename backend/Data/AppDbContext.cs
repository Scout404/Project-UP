using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Admin ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Admin>(e =>
        {
            e.HasKey(a => a.UserId);
            e.Property(a => a.Email).IsRequired();
            e.HasIndex(a => a.Email).IsUnique();
            e.Property(a => a.PasswordHash).IsRequired();
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── Customer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.CustomerId);
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
        });

        // ── Address ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(a => a.AdressId);   // keeping your original spelling
            e.HasOne(a => a.Customer)
             .WithMany(c => c.Addresses)
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Category ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CatId);
            e.Property(c => c.Name).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        // ── Product ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(p => p.ProductId);
            e.Property(p => p.BasePrice).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Color ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Color>(e =>
        {
            e.HasKey(c => c.ColorId);
            e.HasIndex(c => c.Name).IsUnique();
        });

        // ── Size ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Size>(e =>
        {
            e.HasKey(s => s.SizeId);
            e.HasIndex(s => s.Name).IsUnique();
        });

        // ── ProductVariant ────────────────────────────────────────────────────
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.HasKey(v => v.VariantId);
            e.HasOne(v => v.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(v => v.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.Size)
             .WithMany(s => s.Variants)
             .HasForeignKey(v => v.SizeId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Color)
             .WithMany(c => c.Variants)
             .HasForeignKey(v => v.ColorId)
             .OnDelete(DeleteBehavior.Restrict);
            // A product can't have two identical size+color combos
            e.HasIndex(v => new { v.ProductId, v.SizeId, v.ColorId }).IsUnique();
        });

        // ── Orders ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Orders>(e =>
        {
            e.HasKey(o => o.OrderId);
            e.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            e.Property(o => o.Status).HasConversion<string>(); // store enum as text
            e.Property(o => o.OrderDate).HasDefaultValueSql("NOW()");
            e.HasOne(o => o.Customer)
             .WithMany(c => c.Orders)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── OrderAddress ──────────────────────────────────────────────────────
        modelBuilder.Entity<OrderAddress>(e =>
        {
            e.HasKey(oa => oa.OrderId);  // 1-to-1, OrderId is both PK and FK
            e.HasOne(oa => oa.Order)
             .WithOne(o => o.OrderAddress)
             .HasForeignKey<OrderAddress>(oa => oa.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OrderItem ─────────────────────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(oi => oi.OrderItemId);
            e.Property(oi => oi.Price).HasColumnType("decimal(18,2)");
            e.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.Variant)
             .WithMany(v => v.OrderItems)
             .HasForeignKey(oi => oi.VariantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Review ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(r => r.ReviewId);
            e.Property(r => r.Rating).IsRequired();
            // One review per customer per product
            e.HasIndex(r => new { r.CustomerId, r.ProductId }).IsUnique();
            e.HasOne(r => r.Product)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Customer)
             .WithMany(c => c.Reviews)
             .HasForeignKey(r => r.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Wishlist ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Wishlist>(e =>
        {
            e.HasKey(w => w.WishlistId);
            e.HasIndex(w => w.CustomerId).IsUnique(); // one wishlist per customer
            e.HasOne(w => w.Customer)
             .WithOne(c => c.Wishlist)
             .HasForeignKey<Wishlist>(w => w.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── WishlistItem ──────────────────────────────────────────────────────
        modelBuilder.Entity<WishlistItem>(e =>
        {
            e.HasKey(wi => new { wi.WishlistId, wi.VariantId }); // composite PK
            e.HasOne(wi => wi.Wishlist)
             .WithMany(w => w.Items)
             .HasForeignKey(wi => wi.WishlistId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(wi => wi.Variant)
             .WithMany(v => v.WishlistItems)
             .HasForeignKey(wi => wi.VariantId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}