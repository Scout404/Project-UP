using backend.Data;
using backend.Logic;
using backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("Default"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Default"))
    )
);

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();

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
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"), 
                Role = "Admin", 
                Email = "admin@shop.com" 
            },
            new User 
            { 
                Username = "testuser", 
                Password = BCrypt.Net.BCrypt.HashPassword("test123"), 
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
        u.Username == request.Username);
    
    if (user == null || !IsPasswordValid(request.Password, user.Password))
        return Results.Unauthorized();
    
    return Results.Ok(new 
    { 
        id = user.Id, 
        username = user.Username, 
        role = user.Role 
    });
});

// REGISTER ENDPOINT
app.MapPost("/register", async (AppDbContext db, RegisterRequest request) =>
{
    var username = request.Username.Trim();
    var email = request.Email.Trim();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(request.Password) ||
        string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest("Username, email and password are required.");
    }

    var userExists = await db.Users.AnyAsync(u =>
        u.Username == username || u.Email == email);

    if (userExists)
        return Results.Conflict("Username or email already exists.");

    var user = new User
    {
        Username = username,
        Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Email = email,
        Role = "Customer",
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new
    {
        id = user.Id,
        username = user.Username,
        role = user.Role
    });
});

// PRODUCT ENDPOINTS
app.MapGet("/products", async (ProductService service) =>
{
    var products = await service.GetAll();

    return Results.Ok(products);
});

app.MapPost("/products", async (
    ProductService service,
    ProductCreateRequest request) =>
{
    var created = await service.Add(request);

    if (created == null)
        return Results.BadRequest("Invalid category");

    return Results.Ok(created);
});

app.MapPut("/products/{id}", async (ProductService service, int id, ProductUpdateRequest request) =>
{
    var updated = await service.Update(id, request);

    if (!updated)
        return Results.NotFound();

    return Results.Ok();
});

app.MapDelete("/products/{id}", async (ProductService service, int id) =>
{
    var deleted = await service.Delete(id);

    if (!deleted)
        return Results.NotFound();

    return Results.Ok();
});

// CART ENDPOINTS
app.MapGet("/cart/{userId}", async (CartService service, int userId) =>
{
    try
    {
        var cart = await service.GetCart(userId);
        return Results.Ok(cart);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Get cart: {ex.Message}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        return Results.StatusCode(500);
    }
});

app.MapPost("/cart/add/{userId}", async (CartService service, int userId, AddToCartRequest request) =>
{
    try
    {
        var cart = await service.AddToCart(userId, request);

        return Results.Ok(cart);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Add to cart: {ex.Message}");
        return Results.StatusCode(500);
    }
});

app.MapDelete("/cart/remove/{userId}/{variantId}", async ( CartService service, int userId, int variantId) =>
{
    try
    {
        var cart = await service.RemoveFromCart(userId, variantId);

        if (cart == null)
            return Results.NotFound();

        return Results.Ok(cart);
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

static bool IsPasswordValid(string password, string savedPassword)
{
    if (savedPassword.StartsWith("$2"))
        return BCrypt.Net.BCrypt.Verify(password, savedPassword);

    return savedPassword == password;
}
