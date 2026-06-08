using MySqlConnector;

namespace backend.Data;

public class CheckoutRepository
{
    private readonly string _connectionString;

    public CheckoutRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }


    public async Task<int?> GetProductStock(int productId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        string sql = @"SELECT StockQuantity FROM Products WHERE ProductId = @productId LIMIT 1";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
        {
            return null;
        }

        return Convert.ToInt32(result);
    }
    public async Task<bool> ReduceStock(int productId, int quantity)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"UPDATE Products SET StockQuantity = StockQuantity - @quantity WHERE ProductId = @productId AND StockQuantity >= @quantity ";

        using var command = new MySqlCommand(sql, conn);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@quantity", quantity);

        int rows = await command.ExecuteNonQueryAsync();
        return rows > 0;
    }

}