using backend.Models;
using MySqlConnector;

namespace backend.Data;

public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration config)
    {
        _connectionString =
            config.GetConnectionString("Default")!;
    }

    public async Task<User?> GetByUsername(string username)
    {
        using var connection =
            new MySqlConnection(_connectionString);

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

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@username",
            username
        );

        using var reader =
            await command.ExecuteReaderAsync();

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

    public async Task<User?> GetByEmail(string email)
    {
        using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            SELECT *
            FROM Users
            WHERE Email = @email
        ";

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@email", email);

        using var reader =
            await command.ExecuteReaderAsync();

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

    public async Task<int> Create(User user)
    {
        using var connection =
            new MySqlConnection(_connectionString);

        await connection.OpenAsync();

        string sql = @"
            INSERT INTO Users
            (
                Username,
                Password,
                Email,
                Role,
                CreatedAt
            )
            VALUES
            (
                @username,
                @password,
                @email,
                @role,
                @createdAt
            );

            SELECT LAST_INSERT_ID();
        ";

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@username",
            user.Username
        );

        command.Parameters.AddWithValue(
            "@password",
            user.Password
        );

        command.Parameters.AddWithValue(
            "@email",
            user.Email
        );

        command.Parameters.AddWithValue(
            "@role",
            user.Role
        );

        command.Parameters.AddWithValue(
            "@createdAt",
            user.CreatedAt
        );

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}