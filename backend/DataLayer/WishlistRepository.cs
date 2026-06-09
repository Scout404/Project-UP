using MySqlConnector;

namespace backend.Data;

public class WishlistRepository
{
    private readonly string _connectionString;

    public WishlistRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task Add(int userId, int productId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        //Check if favorite already exists
        string checkSql = @" SELECT COUNT(*) FROM WishlistItems WHERE UserId = @userId AND ProductId = @productId;";

        using (var checkCmd = new MySqlCommand(checkSql, conn))
        {
            checkCmd.Parameters.AddWithValue("@userId", userId);
            checkCmd.Parameters.AddWithValue("@productId", productId);

            var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (exists > 0)
            {
                return; // 
            }
        }

        // insert if it doesnt exist
        string insertSql = @"INSERT INTO WishlistItems (UserId, ProductId) VALUES (@userId, @productId);";

        using var insertCmd = new MySqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("@userId", userId);
        insertCmd.Parameters.AddWithValue("@productId", productId);

        await insertCmd.ExecuteNonQueryAsync();
    }

    public async Task Remove(int userId, int productId)
    {
        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"DELETE FROM WishlistItems WHERE UserId = @userId AND ProductId = @productId;
        ";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@productId", productId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<int>> GetWishlistProductIds(int userId)
    {
        var list = new List<int>();

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        string sql = @"SELECT ProductId FROM WishlistItems WHERE UserId = @userId;";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(reader.GetInt32("ProductId"));
        }

        return list;
    }
}
