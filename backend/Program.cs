using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
    builder.Configuration.GetConnectionString("Default"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Default"))
));
    
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
}

app.MapPost("/login", (AppDbContext db, LoginRequest request) =>
{
    var user = db.Users.FirstOrDefault(u =>
        u.Username == request.Username &&
        u.Password == request.Password);

    if (user == null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        role = user.Role
    });
});

app.MapGet("/products", (AppDbContext db) =>
{
    return db.Products.ToList();
});

app.MapPost("/products", (AppDbContext db, Product product) =>
{
    db.Products.Add(product);
    db.SaveChanges();
    return Results.Ok(product);
});

app.MapPut("/products/{id}", (AppDbContext db, int id, Product updated) =>
{
    var product = db.Products.Find(id);
    if (product == null) return Results.NotFound();

    product.Name = updated.Name;
    product.BasePrice = updated.BasePrice;
    product.Stock = updated.Stock;
    product.Category = updated.Category;
    product.IsActive = updated.IsActive;

    db.SaveChanges();
    return Results.Ok(product);
});

app.MapDelete("/products/{id}", (AppDbContext db, int id) =>
{
    var product = db.Products.Find(id);
    if (product == null) return Results.NotFound();

    db.Products.Remove(product);
    db.SaveChanges();
    return Results.Ok();
});

app.MapGet("/cart/{userId}", (AppDbContext db, int userId) =>
{
    var cart = db.Carts
        .Include(c => c.Items)
        .FirstOrDefault(c => c.UserId == userId);

    if (cart == null)
    {
        cart = new Cart { UserId = userId, Items = new List<CartItem>() };
        db.Carts.Add(cart);
        db.SaveChanges();
    }

    var cartDto = new CartDto
    {
        UserId = cart.UserId,
        Items = cart.Items.Select(i => new CartItemDto
        {
            VariantId = i.VariantId,
            Name = i.Name,
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
                Name = req.Name,
                Price = req.Price,
                Quantity = req.Quantity
            });
        }

        db.SaveChanges();

        // Refetch and convert to DTO
        var updatedCart = db.Carts
            .Include(c => c.Items)
            .FirstOrDefault(c => c.Id == cart.Id);

        var cartDto = new CartDto
        {
            UserId = updatedCart!.UserId,
            Items = updatedCart.Items.Select(i => new CartItemDto
            {
                VariantId = i.VariantId,
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        return Results.Ok(cartDto);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
        return Results.StatusCode(500);
    }
});

app.MapDelete("/cart/remove/{userId}/{variantId}", (AppDbContext db, int userId, int variantId) =>
{
    var cart = db.Carts
        .Include(c => c.Items)
        .FirstOrDefault(c => c.UserId == userId);

    if (cart == null) return Results.NotFound();

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
            Name = i.Name,
            Price = i.Price,
            Quantity = i.Quantity
        }).ToList()
    };

    return Results.Ok(cartDto);
});

app.MapGet("/searchFunc", (string? searchedProduct,int? categoryId,string? brand,decimal? minPrice,decimal? maxPrice,int? colorId,int? sizeId) =>
{
    var search = new SearchFunction();

    return search.Search(searchedProduct,categoryId,brand, minPrice,maxPrice,colorId,sizeId);
});

app.Run();