using MySqlConnector;

namespace backend.Data;

public class CartRepository
{
    private readonly string _connectionString;

    public CartRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task<CartDto?> GetCart(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                c.Id AS CartId,
                c.UserId,
                ci.Id AS CartItemId,
                ci.VariantId,
                ci.Name,
                ci.Price,
                ci.Quantity
            FROM Carts c
            LEFT JOIN CartItems ci
                ON c.Id = ci.CartId
            WHERE c.UserId = @userId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@userId", userId);

        using var reader = await command.ExecuteReaderAsync();

        var items = new List<CartItemDto>();

        bool hasCart = false;

        while (await reader.ReadAsync())
        {
            hasCart = true;

            if (reader["CartItemId"] != DBNull.Value)
            {
                items.Add(new CartItemDto
                {
                    VariantId = Convert.ToInt32(reader["VariantId"]),
                    Name = reader["Name"]?.ToString() ?? "Unknown",
                    Price = reader["Price"] != DBNull.Value
                        ? Convert.ToDecimal(reader["Price"])
                        : 0,

                    Quantity = reader["Quantity"] != DBNull.Value
                        ? Convert.ToInt32(reader["Quantity"])
                        : 0
                });
            }
        }

        if (!hasCart)
            return null;

        return new CartDto
        {
            UserId = userId,
            Items = items
        };
    }

    public async Task<int> CreateCart(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            INSERT INTO Carts (UserId)
            VALUES (@userId);
        ";
        
        using var createCommand = new MySqlCommand(sql, connection);

        createCommand.Parameters.AddWithValue("@userId", userId);

        await createCommand.ExecuteNonQueryAsync();

        int cartId = (int)createCommand.LastInsertedId;
        return cartId;
    }

    public async Task<int?> GetCartId(int userId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT Id
            FROM Carts
            WHERE UserId = @userId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@userId", userId);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return null;

        return Convert.ToInt32(result);
    }

    public async Task<Product?> GetProduct(int productId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                ProductId,
                Name,
                BasePrice
            FROM Products
            WHERE ProductId = @productId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@productId", productId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Product
            {
                ProductId = Convert.ToInt32(reader["ProductId"]),
                Name = reader["Name"].ToString() ?? "Unknown",
                BasePrice = Convert.ToDecimal(reader["BasePrice"])
            };
        }

        return null;
    }

    public async Task<CartItem?> GetCartItem(int cartId, int variantId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                Id AS CartItemId,
                VariantId,
                Name,
                Price,
                Quantity
            FROM CartItems
            WHERE CartId = @cartId
            AND VariantId = @variantId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@cartId", cartId);
        command.Parameters.AddWithValue("@variantId", variantId);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new CartItem
            {
                Id = Convert.ToInt32(reader["CartItemId"]),
                VariantId = Convert.ToInt32(reader["VariantId"]),
                Name = reader["Name"].ToString(),
                Price = Convert.ToDecimal(reader["Price"]),
                Quantity = Convert.ToInt32(reader["Quantity"])
            };
        }

        return null;
    }

    public async Task<bool> UpdateCartItemQuantity(
        int cartId,
        int variantId,
        int quantity)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            UPDATE CartItems
            SET Quantity = @quantity
            WHERE CartId = @cartId
            AND VariantId = @variantId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@cartId", cartId);
        command.Parameters.AddWithValue("@variantId", variantId);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }

    public async Task<bool> AddCartItem(
        int cartId,
        int variantId,
        string name,
        decimal price,
        int quantity)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            INSERT INTO CartItems
            (
                CartId,
                VariantId,
                Name,
                Price,
                Quantity
            )
            VALUES
            (
                @cartId,
                @variantId,
                @name,
                @price,
                @quantity
            )
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@cartId", cartId);
        command.Parameters.AddWithValue("@variantId", variantId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@quantity", quantity);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }

    public async Task<bool> RemoveCartItem(int cartId, int variantId)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            DELETE FROM CartItems
            WHERE CartId = @cartId
            AND VariantId = @variantId
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@cartId", cartId);
        command.Parameters.AddWithValue("@variantId", variantId);

        int rows = await command.ExecuteNonQueryAsync();

        return rows > 0;
    }
}
