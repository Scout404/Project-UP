using backend.Models;
using backend.Logic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck");

// Login endpoint
app.MapPost("/login", (LoginRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Username and password are required." });
    }

    AuthenticationService authService = new AuthenticationService();
    var user = authService.Authenticate(request.Username, request.Password);

    if (user != null)
    {
        return Results.Ok(new { success = true, message = "Login successful!", role = user.Role, username = user.Username });
    }
    else
    {
        return Results.Unauthorized();
    }
})
.WithName("Login")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/searchFunc", (string? searchedProduct,int? categoryId,string? brand,decimal? minPrice,decimal? maxPrice,int? colorId,int? sizeId) =>
{
    var search = new SearchFunction();

    return search.Search(searchedProduct,categoryId,brand, minPrice,maxPrice,colorId,sizeId);
});

app.Run();
