using MySqlConnector;

namespace backend.Data;

public class ProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task<List<ProductDto>> GetAll()
    {
        var products = new List<ProductDto>();

        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                p.ProductId,
                p.Name,
                p.Description,
                p.Brand,
                p.BasePrice,
                p.CategoryId,
                c.Name AS CategoryName,
                p.IsActive,
                p.StockQuantity
            FROM Products p
            LEFT JOIN Categories c
                ON p.CategoryId = c.CategoryId
        ";

        using var command = new MySqlCommand(sql, connection);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new ProductDto
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                Name = reader["Name"].ToString() ?? "",
                Description = reader["Description"].ToString() ?? "",
                Brand = reader["Brand"].ToString() ?? "",
                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                CategoryName = reader["CategoryName"].ToString() ?? "",
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                StockQuantity = Convert.ToInt32(reader["StockQuantity"])
            });
        }

        return products;
    }

    public async Task<ProductDto?> GetById(int id)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                p.ProductId,
                p.Name,
                p.Description,
                p.Brand,
                p.BasePrice,
                p.CategoryId,
                c.Name AS CategoryName,
                p.IsActive,
                p.StockQuantity
            FROM Products p
            LEFT JOIN Categories c
                ON p.CategoryId = c.CategoryId
            WHERE p.ProductId = @id
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ProductDto
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                Name = reader["Name"].ToString() ?? "",
                Description = reader["Description"].ToString() ?? "",
                Brand = reader["Brand"].ToString() ?? "",
                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                CategoryId = Convert.ToInt32(reader["CategoryId"]),
                CategoryName = reader["CategoryName"].ToString() ?? "",
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                StockQuantity = Convert.ToInt32(reader["StockQuantity"])
            };
        }

        return null;
    }

    public async Task<ProductDto?> Add(ProductCreateRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        if (!await CategoryExists(connection, request.CategoryId))
            return null;

        string sql = @"
            INSERT INTO Products
            (
                Name,
                Description,
                Brand,
                BasePrice,
                CategoryId,
                IsActive,
                StockQuantity
            )
            VALUES
            (
                @name,
                @description,
                @brand,
                @basePrice,
                @categoryId,
                @isActive,
                @stockQuantity
            )
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@name", request.Name?.Trim() ?? "");
        command.Parameters.AddWithValue("@description", request.Description?.Trim() ?? "");
        command.Parameters.AddWithValue("@brand", request.Brand?.Trim() ?? "");
        command.Parameters.AddWithValue("@basePrice", request.BasePrice);
        command.Parameters.AddWithValue("@categoryId", request.CategoryId);
        command.Parameters.AddWithValue("@isActive", request.IsActive);
        command.Parameters.AddWithValue("@stockQuantity", request.StockQuantity);

        await command.ExecuteNonQueryAsync();

        int productId = Convert.ToInt32(command.LastInsertedId);

        return await GetById(productId);
    }

    private static async Task<bool> CategoryExists(MySqlConnection connection, int categoryId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM Categories
            WHERE CategoryId = @categoryId
        ";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@categoryId", categoryId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task<bool> Update(int id, ProductUpdateRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            UPDATE Products
            SET
                Name = @name,
                Description = @description,
                Brand = @brand,
                BasePrice = @basePrice,
                CategoryId = @categoryId,
                IsActive = @isActive,
                StockQuantity = @stockQuantity
            WHERE ProductId = @id
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@description", request.Description);
        command.Parameters.AddWithValue("@brand", request.Brand);
        command.Parameters.AddWithValue("@basePrice", request.BasePrice);
        command.Parameters.AddWithValue("@categoryId", request.CategoryId);
        command.Parameters.AddWithValue("@isActive", request.IsActive);
        command.Parameters.AddWithValue("@stockQuantity", request.StockQuantity);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }

    public async Task<bool> Delete(int id)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            DELETE FROM Products
            WHERE ProductId = @id
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }

    public async Task<int?> GetStock(int productId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT StockQuantity
            FROM Products
            WHERE ProductId = @productId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@productId", productId);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return null;

        return Convert.ToInt32(result);
    }

    public async Task<bool> SetStock(int productId, int quantity)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            UPDATE Products
            SET StockQuantity = @quantity
            WHERE ProductId = @productId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@productId", productId);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }
}
