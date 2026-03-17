using Webshop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

app.MapGet("/test-db", async (DatabaseService db) =>
{
    using var conn = db.GetConnection();
    await conn.OpenAsync();
    return "Database connected!";
});