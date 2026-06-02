using backend.Models;
using MySqlConnector;

namespace backend.Data;

public enum ReviewDeleteResult
{
    Deleted,
    NotFound,
    Forbidden
}

public class ReviewRepository
{
    private readonly string _connectionString;

    public ReviewRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task<List<ReviewDto>> GetByProductId(int productId)
    {
        var reviews = new List<ReviewDto>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var hasCreatedAt = await ColumnExists(connection, "Reviews", "CreatedAt");
        var createdAtSelect = hasCreatedAt ? "r.CreatedAt" : "NULL";

        var sql = $@"
            SELECT
                r.ReviewId,
                r.ProductId,
                u.Id AS UserId,
                r.ReviewText,
                r.Rating,
                {createdAtSelect} AS CreatedAt,
                COALESCE(
                    NULLIF(u.Username, ''),
                    NULLIF(TRIM(CONCAT(COALESCE(c.FirstName, ''), ' ', COALESCE(c.LastName, ''))), ''),
                    NULLIF(c.Email, ''),
                    CONCAT('Customer ', r.CustomerId)
                ) AS ReviewerName
            FROM Reviews r
            LEFT JOIN Customer c
                ON r.CustomerId = c.CustomerId
            LEFT JOIN Users u
                ON c.Email = u.Email
            WHERE r.ProductId = @productId
            ORDER BY r.ReviewId DESC
        ";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            reviews.Add(new ReviewDto
            {
                ReviewId = Convert.ToInt32(reader["ReviewId"]),
                ProductId = Convert.ToInt32(reader["ProductId"]),
                UserId = reader["UserId"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["UserId"]),
                ReviewerName = reader["ReviewerName"].ToString() ?? "Customer",
                ReviewText = reader["ReviewText"].ToString() ?? "",
                Rating = Math.Clamp(Convert.ToInt32(reader["Rating"]), 1, 5),
                CreatedAt = reader["CreatedAt"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["CreatedAt"])
            });
        }

        return reviews;
    }

    public async Task<ReviewDto?> Add(int productId, User user, string reviewText, int rating)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        if (!await ProductExists(connection, productId))
        {
            return null;
        }

        var hasCreatedAt = await ColumnExists(connection, "Reviews", "CreatedAt");
        var customerId = await EnsureCustomerForUser(connection, user);
        var createdAt = DateTime.UtcNow;

        var sql = hasCreatedAt
            ? @"
                INSERT INTO Reviews
                (
                    ProductId,
                    CustomerId,
                    ReviewText,
                    Rating,
                    CreatedAt
                )
                VALUES
                (
                    @productId,
                    @customerId,
                    @reviewText,
                    @rating,
                    @createdAt
                );

                SELECT LAST_INSERT_ID();
            "
            : @"
                INSERT INTO Reviews
                (
                    ProductId,
                    CustomerId,
                    ReviewText,
                    Rating
                )
                VALUES
                (
                    @productId,
                    @customerId,
                    @reviewText,
                    @rating
                );

                SELECT LAST_INSERT_ID();
            ";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@customerId", customerId);
        command.Parameters.AddWithValue("@reviewText", reviewText);
        command.Parameters.AddWithValue("@rating", rating);

        if (hasCreatedAt)
        {
            command.Parameters.AddWithValue("@createdAt", createdAt);
        }

        var reviewId = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new ReviewDto
        {
            ReviewId = reviewId,
            ProductId = productId,
            UserId = user.Id,
            ReviewerName = user.Username,
            ReviewText = reviewText,
            Rating = rating,
            CreatedAt = hasCreatedAt ? createdAt : null
        };
    }

    public async Task<ReviewDeleteResult> DeleteOwnReview(int productId, int reviewId, User user)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string ownerSql = @"
            SELECT c.Email
            FROM Reviews r
            INNER JOIN Customer c
                ON r.CustomerId = c.CustomerId
            WHERE r.ProductId = @productId
                AND r.ReviewId = @reviewId
            LIMIT 1
        ";

        string? ownerEmail;

        await using (var ownerCommand = new MySqlCommand(ownerSql, connection))
        {
            ownerCommand.Parameters.AddWithValue("@productId", productId);
            ownerCommand.Parameters.AddWithValue("@reviewId", reviewId);

            var ownerResult = await ownerCommand.ExecuteScalarAsync();

            if (ownerResult == null || ownerResult == DBNull.Value)
            {
                return ReviewDeleteResult.NotFound;
            }

            ownerEmail = ownerResult.ToString();
        }

        if (!string.Equals(ownerEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return ReviewDeleteResult.Forbidden;
        }

        const string deleteSql = @"
            DELETE FROM Reviews
            WHERE ProductId = @productId
                AND ReviewId = @reviewId
        ";

        await using var deleteCommand = new MySqlCommand(deleteSql, connection);
        deleteCommand.Parameters.AddWithValue("@productId", productId);
        deleteCommand.Parameters.AddWithValue("@reviewId", reviewId);

        var deletedRows = await deleteCommand.ExecuteNonQueryAsync();

        return deletedRows > 0
            ? ReviewDeleteResult.Deleted
            : ReviewDeleteResult.NotFound;
    }

    private static async Task<bool> ProductExists(MySqlConnection connection, int productId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM Products
            WHERE ProductId = @productId
        ";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@productId", productId);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static async Task<int> EnsureCustomerForUser(MySqlConnection connection, User user)
    {
        const string findSql = @"
            SELECT CustomerId
            FROM Customer
            WHERE Email = @email
            LIMIT 1
        ";

        await using (var findCommand = new MySqlCommand(findSql, connection))
        {
            findCommand.Parameters.AddWithValue("@email", user.Email);

            var existingCustomerId = await findCommand.ExecuteScalarAsync();

            if (existingCustomerId != null && existingCustomerId != DBNull.Value)
            {
                return Convert.ToInt32(existingCustomerId);
            }
        }

        const string createSql = @"
            INSERT INTO Customer
            (
                FirstName,
                LastName,
                Email,
                PasswordHash,
                Phone,
                IsMember,
                CreatedAt
            )
            VALUES
            (
                NULL,
                NULL,
                @email,
                NULL,
                NULL,
                1,
                @createdAt
            );

            SELECT LAST_INSERT_ID();
        ";

        await using var createCommand = new MySqlCommand(createSql, connection);
        createCommand.Parameters.AddWithValue("@email", user.Email);
        createCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

        return Convert.ToInt32(await createCommand.ExecuteScalarAsync());
    }

    private static async Task<bool> ColumnExists(MySqlConnection connection, string tableName, string columnName)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = @tableName
                AND column_name = @columnName
        ";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }
}
