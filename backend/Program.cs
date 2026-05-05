using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Default"))
    )
);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User 
            { 
                Username = "admin", 
                Password = "admin123", 
                Role = "Admin", 
                Email = "admin@shop.com" 
            },
            new User 
            { 
                Username = "testuser", 
                Password = "test123", 
                Role = "Customer", 
                Email = "test@shop.com" 
            }
        );
        db.SaveChanges();
    }

    // Default categories
    if (!db.Categories.Any())
    {
        Console.WriteLine("[SEED] Creating categories...");
        db.Categories.AddRange(
            new Category { Name = "home" },
            new Category { Name = "clothes" },
            new Category { Name = "accessoires" },
            new Category { Name = "collections" }
        );
        db.SaveChanges();
        Console.WriteLine("[SEED] Categories created successfully");
    }
}

// LOGIN ENDPOINT
app.MapPost("/login", (AppDbContext db, LoginRequest request) =>
{
    var user = db.Users.FirstOrDefault(u => 
        u.Username == request.Username && u.Password == request.Password);
    
    if (user == null)
        return Results.Unauthorized();
    
    return Results.Ok(new 
    { 
        id = user.Id, 
        username = user.Username, 
        role = user.Role 
    });
});

// PRODUCT ENDPOINTS
app.MapGet("/products", (AppDbContext db) =>
{
    var products = db.Products
        .Include(p => p.Category)
        .Select(p => new ProductDto 
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Brand = p.Brand,
            BasePrice = p.BasePrice,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            IsActive = p.IsActive
        })
        .ToList();
    
    Console.WriteLine($"[GET /products] Returning {products.Count} products");
    return Results.Ok(products);
});

app.MapPost("/products", (AppDbContext db, ProductCreateRequest request) =>
{
    try
    {
        var category = db.Categories.Find(request.CategoryId);
        if (category == null) return Results.BadRequest(new { error = "Category not found" });

        var product = new Product
        {
            Name = request.Name ?? "Unnamed Product",
            Description = request.Description ?? "",
            Brand = request.Brand ?? "Unknown",
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive
        };

        db.Products.Add(product);
        db.SaveChanges();

        return Results.Ok(new { 
            productId = product.ProductId, 
            name = product.Name,
            message = "Created successfully" 
        });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(500);
    }
});

app.MapPut("/products/{id}", (AppDbContext db, int id, ProductUpdateRequest request) =>
{
    try
    {
        var product = db.Products.Find(id);
        if (product == null)
            return Results.NotFound();

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.Brand != null) product.Brand = request.Brand;
        if (request.BasePrice.HasValue) product.BasePrice = request.BasePrice.Value;
        if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        db.SaveChanges();
        return Results.Ok(product);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Update product: {ex.Message}");
        return Results.StatusCode(500);
    }
});

app.MapDelete("/products/{id}", (AppDbContext db, int id) =>
{
    try
    {
        var product = db.Products.Find(id);
        if (product == null)
            return Results.NotFound();

        db.Products.Remove(product);
        db.SaveChanges();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Delete product: {ex.Message}");
        return Results.StatusCode(500);
    }
});

// CART ENDPOINTS
app.MapGet("/cart/{userId}", (AppDbContext db, int userId) =>
{
    var cart = db.Carts
        .Include(c => c.Items)
        .FirstOrDefault(c => c.UserId == userId);

    if (cart == null)
    {
        cart = new Cart 
        { 
            UserId = userId, 
            Items = new List<CartItem>() 
        };
        db.Carts.Add(cart);
        db.SaveChanges();
    }

    var cartDto = new CartDto
    {
        UserId = cart.UserId,
        Items = cart.Items.Select(i => new CartItemDto
        {
            VariantId = i.VariantId,
            Name = i.Name ?? "Unknown",
            Price = i.Price,
            Quantity = i.Quantity
        }).ToList()
    };

    return Results.Ok(cartDto);
});

app.MapPost("/cart/add/{userId}", (AppDbContext db, int userId, AddToCartRequest req) =>
{
    try
    {
        var cart = db.Carts.FirstOrDefault(c => c.UserId == userId);
        
        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            db.Carts.Add(cart);
            db.SaveChanges();
        }

        var existing = db.CartItems
            .FirstOrDefault(x => x.CartId == cart.Id && x.VariantId == req.VariantId);

        if (existing != null)
        {
            existing.Quantity += req.Quantity;
        }
        else
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                VariantId = req.VariantId,
                Name = req.Name ?? "Unknown",
                Price = req.Price,
                Quantity = req.Quantity
            });
        }

        db.SaveChanges();

        var updatedCart = db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.Id == cart.Id);

        var cartDto = new CartDto
        {
            UserId = updatedCart!.UserId,
            Items = updatedCart.Items.Select(i => new CartItemDto
            {
                VariantId = i.VariantId,
                Name = i.Name ?? "Unknown",
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        return Results.Ok(cartDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Add to cart: {ex.Message}");
        return Results.StatusCode(500);
    }
});

app.MapDelete("/cart/remove/{userId}/{variantId}", (AppDbContext db, int userId, int variantId) =>
{
    try
    {
        var cart = db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null)
            return Results.NotFound();

        var item = cart.Items.FirstOrDefault(x => x.VariantId == variantId);
        if (item != null)
        {
            db.CartItems.Remove(item);
            db.SaveChanges();
        }

        var updatedCart = db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.Id == cart.Id);

        var cartDto = new CartDto
        {
            UserId = updatedCart!.UserId,
            Items = updatedCart.Items.Select(i => new CartItemDto
            {
                VariantId = i.VariantId,
                Name = i.Name ?? "Unknown",
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        return Results.Ok(cartDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Remove from cart: {ex.Message}");
        return Results.StatusCode(500);
    }
});

// SEARCH FUNCTION
app.MapGet("/searchFunc", (string? searchedProduct, int? categoryId, string? brand, 
    decimal? minPrice, decimal? maxPrice, int? colorId, int? sizeId) =>
{
    var search = new SearchFunction();
    return search.Search(searchedProduct, categoryId, brand, minPrice, maxPrice, colorId, sizeId);
});

// DEBUG ENDPOINTS
app.MapGet("/debug/table-counts", (AppDbContext db) =>
{
    var counts = new 
    { 
        usersCount = db.Users.Count(),
        categoriesCount = db.Categories.Count(),
        productsCount = db.Products.Count(),
        cartsCount = db.Carts.Count(),
        cartItemsCount = db.CartItems.Count()
    };
    Console.WriteLine($"[DEBUG] Table counts: {System.Text.Json.JsonSerializer.Serialize(counts)}");
    return Results.Ok(counts);
});

app.MapGet("/debug/categories", (AppDbContext db) =>
{
    var categories = db.Categories
        .Select(c => new { c.CategoryId, c.Name })
        .ToList();
    Console.WriteLine($"[DEBUG] Categories: {System.Text.Json.JsonSerializer.Serialize(categories)}");
    return Results.Ok(new { totalCategories = categories.Count, categories });
});

app.MapGet("/debug/products-full", (AppDbContext db) =>
{
    var products = db.Products
        .Include(p => p.Category)
        .Select(p => new 
        { 
            p.ProductId, 
            p.Name, 
            p.Brand, 
            p.BasePrice,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name
        })
        .ToList();
    Console.WriteLine($"[DEBUG] Products: {System.Text.Json.JsonSerializer.Serialize(products)}");
    return Results.Ok(new { totalProducts = products.Count, products });
});

app.Run();