using backend.Data;
using backend.Logic;
using backend.Models;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<ReviewRepository>();
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
var frontendBuildPath = Path.GetFullPath(
    Path.Combine(app.Environment.ContentRootPath, "..", "frontend", "build"));

app.UseCors("AllowAll");
app.UseHttpsRedirection();

if (Directory.Exists(frontendBuildPath))
{
    var frontendFiles = new PhysicalFileProvider(frontendBuildPath);

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFiles
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFiles
    });
}

// Database schema and seeding
using (var scope = app.Services.CreateScope())
{
    var connectionString = app.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing Default connection string.");

    await ExecuteSchemaSqlIfDatabaseIsEmpty(connectionString);

    if (!await AnyRows(connectionString, "Users"))
    {
        await ExecuteNonQuery(
            connectionString,
            @"
                INSERT INTO Users (Username, Password, Email, Role, CreatedAt)
                VALUES
                    (@adminUsername, @adminPassword, @adminEmail, @adminRole, @createdAt),
                    (@testUsername, @testPassword, @testEmail, @testRole, @createdAt);
            ",
            command =>
            {
                command.Parameters.AddWithValue("@adminUsername", "admin");
                command.Parameters.AddWithValue("@adminPassword", BCrypt.Net.BCrypt.HashPassword("admin123"));
                command.Parameters.AddWithValue("@adminEmail", "admin@shop.com");
                command.Parameters.AddWithValue("@adminRole", "Admin");
                command.Parameters.AddWithValue("@testUsername", "testuser");
                command.Parameters.AddWithValue("@testPassword", BCrypt.Net.BCrypt.HashPassword("test123"));
                command.Parameters.AddWithValue("@testEmail", "test@shop.com");
                command.Parameters.AddWithValue("@testRole", "Customer");
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
            });
    }

    // Default categories
    if (!await AnyRows(connectionString, "Categories"))
    {
        Console.WriteLine("[SEED] Creating categories...");
        await ExecuteNonQuery(
            connectionString,
            @"
                INSERT INTO Categories (Name)
                VALUES
                    (@home),
                    (@clothes),
                    (@accessoires),
                    (@collections);
            ",
            command =>
            {
                command.Parameters.AddWithValue("@home", "home");
                command.Parameters.AddWithValue("@clothes", "clothes");
                command.Parameters.AddWithValue("@accessoires", "accessoires");
                command.Parameters.AddWithValue("@collections", "collections");
            });
        Console.WriteLine("[SEED] Categories created successfully");
    }
}

// LOGIN ENDPOINT
app.MapPost("/login",
    async (
        AuthenticationService auth,
        LoginRequest request
    ) =>
{
    var user = await auth.Authenticate(
        request.Username,
        request.Password
    );

    if (user == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        id = user.Id,
        username = user.Username,
        role = user.Role
    });
});

// REGISTER ENDPOINT
app.MapPost("/register",
    async (
        AuthenticationService auth,
        RegisterRequest request
    ) =>
{
    var result = await auth.Register(
        request.Username.Trim(),
        request.Email.Trim(),
        request.Password
    );

    if (!result.Success)
    {
        return Results.Conflict(result.Error);
    }

    return Results.Ok(new
    {
        id = result.User!.Id,
        username = result.User.Username,
        role = result.User.Role
    });
});

// PRODUCT ENDPOINTS
app.MapGet("/products", async (ProductService service) =>
{
    var products = await service.GetAll();

    return Results.Ok(products);
});

app.MapGet("/products/{id}", async (ProductService service, int id) =>
{
    var product = await service.GetById(id);

    if (product == null)
        return Results.NotFound();

    return Results.Ok(product);
});

app.MapGet("/products/{productId}/reviews", async (ReviewRepository reviews, int productId) =>
{
    var productReviews = await reviews.GetByProductId(productId);

    return Results.Ok(productReviews);
});

app.MapPost("/products/{productId}/reviews", async (
    ReviewRepository reviews,
    UserRepository users,
    int productId,
    ReviewCreateRequest request) =>
{
    var reviewText = request.ReviewText?.Trim();

    if (request.UserId <= 0 || string.IsNullOrWhiteSpace(reviewText))
    {
        return Results.BadRequest("User and review text are required.");
    }

    if (request.Rating < 1 || request.Rating > 5)
    {
        return Results.BadRequest("Rating must be between 1 and 5.");
    }

    var user = await users.GetById(request.UserId);

    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Forbid();
    }

    var created = await reviews.Add(productId, user, reviewText, request.Rating);

    if (created == null)
    {
        return Results.NotFound("Product not found.");
    }

    return Results.Created($"/products/{productId}/reviews", created);
});

app.MapDelete("/products/{productId}/reviews/{reviewId}/users/{userId}", async (
    ReviewRepository reviews,
    UserRepository users,
    int productId,
    int reviewId,
    int userId) =>
{
    if (userId <= 0 || reviewId <= 0)
    {
        return Results.BadRequest("User and review are required.");
    }

    var user = await users.GetById(userId);

    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    if (!string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Forbid();
    }

    var result = await reviews.DeleteOwnReview(productId, reviewId, user);

    return result switch
    {
        ReviewDeleteResult.Deleted => Results.Ok(),
        ReviewDeleteResult.Forbidden => Results.Forbid(),
        _ => Results.NotFound("Review not found.")
    };
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
app.MapGet("/debug/table-counts", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing Default connection string.");

    var counts = new
    {
        usersCount = await CountRows(connectionString, "Users"),
        categoriesCount = await CountRows(connectionString, "Categories"),
        productsCount = await CountRows(connectionString, "Products"),
        cartsCount = await CountRows(connectionString, "Carts"),
        cartItemsCount = await CountRows(connectionString, "CartItems")
    };

    Console.WriteLine($"[DEBUG] Table counts: {System.Text.Json.JsonSerializer.Serialize(counts)}");
    return Results.Ok(counts);
});

app.MapGet("/debug/categories", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing Default connection string.");

    var categories = new List<object>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new MySqlCommand("SELECT CategoryId, Name FROM Categories", connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        categories.Add(new
        {
            CategoryId = reader.GetInt32("CategoryId"),
            Name = reader.GetString("Name")
        });
    }

    Console.WriteLine($"[DEBUG] Categories: {System.Text.Json.JsonSerializer.Serialize(categories)}");
    return Results.Ok(new { totalCategories = categories.Count, categories });
});

app.MapGet("/debug/products-full", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing Default connection string.");

    var products = new List<object>();

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new MySqlCommand(
        @"
            SELECT
                p.ProductId,
                p.Name,
                p.Brand,
                p.BasePrice,
                p.CategoryId,
                c.Name AS CategoryName
            FROM Products p
            LEFT JOIN Categories c
                ON p.CategoryId = c.CategoryId
        ",
        connection);
    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        products.Add(new
        {
            ProductId = reader.GetInt32("ProductId"),
            Name = reader.GetString("Name"),
            Brand = reader.GetString("Brand"),
            BasePrice = reader.GetDecimal("BasePrice"),
            CategoryId = reader.GetInt32("CategoryId"),
            CategoryName = reader["CategoryName"]?.ToString() ?? ""
        });
    }

    Console.WriteLine($"[DEBUG] Products: {System.Text.Json.JsonSerializer.Serialize(products)}");
    return Results.Ok(new { totalProducts = products.Count, products });
});

if (Directory.Exists(frontendBuildPath))
{
    app.MapFallback(async context =>
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(Path.Combine(frontendBuildPath, "index.html"));
    });
}

app.Run();

static async Task ExecuteSchemaSqlIfDatabaseIsEmpty(string connectionString)
{
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE();";

    var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync());

    if (tableCount > 0)
    {
        return;
    }

    var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");

    if (!File.Exists(schemaPath))
    {
        schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "schema.sql");
    }

    if (!File.Exists(schemaPath))
    {
        schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "backend", "schema.sql");
    }

    if (!File.Exists(schemaPath))
    {
        throw new FileNotFoundException("Could not find schema.sql.", schemaPath);
    }

    var schemaSql = File.ReadAllText(schemaPath);
    var statements = Regex.Split(schemaSql, @";\s*(?:\r?\n|$)")
        .Select(statement => statement.Trim())
        .Where(statement => !string.IsNullOrWhiteSpace(statement));

    foreach (var statement in statements)
    {
        await using var schemaCommand = new MySqlCommand(statement, connection);
        await schemaCommand.ExecuteNonQueryAsync();
    }
}

static async Task<bool> AnyRows(string connectionString, string tableName)
{
    return await CountRows(connectionString, tableName) > 0;
}

static async Task<int> CountRows(string connectionString, string tableName)
{
    var allowedTables = new HashSet<string>
    {
        "Users",
        "Categories",
        "Products",
        "Carts",
        "CartItems"
    };

    if (!allowedTables.Contains(tableName))
    {
        throw new ArgumentException("Table is not allowed.", nameof(tableName));
    }

    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new MySqlCommand($"SELECT COUNT(*) FROM `{tableName}`", connection);
    return Convert.ToInt32(await command.ExecuteScalarAsync());
}

static async Task ExecuteNonQuery(string connectionString, string sql, Action<MySqlCommand> configureCommand)
{
    await using var connection = new MySqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new MySqlCommand(sql, connection);
    configureCommand(command);
    await command.ExecuteNonQueryAsync();
}
