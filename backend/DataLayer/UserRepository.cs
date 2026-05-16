using backend.Models;
using MySqlConnector;

namespace backend.Data;

public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task<User?> GetByUsername(string username)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT
                Id,
                Username,
                Password,
                Email,
                Role,
                CreatedAt
            FROM Users
            WHERE Username = @username
        ";

        using var command = new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@username", username);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Username = reader["Username"].ToString() ?? "",
                Password = reader["Password"].ToString() ?? "",
                Email = reader["Email"].ToString() ?? "",
                Role = reader["Role"].ToString() ?? "",
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        return null;
    }
}