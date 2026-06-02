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
                p.StockQuantity,
                (
                    SELECT pv.PictureUrl
                    FROM ProductVariants pv
                    WHERE pv.ProductId = p.ProductId
                        AND pv.PictureUrl IS NOT NULL
                        AND pv.PictureUrl <> ''
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS ImageUrl,
                (
                    SELECT col.Name
                    FROM ProductVariants pv
                    JOIN Colors col ON pv.ColorId = col.ColorId
                    WHERE pv.ProductId = p.ProductId
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS ColorName,
                (
                    SELECT s.Name
                    FROM ProductVariants pv
                    JOIN Sizes s ON pv.SizeId = s.SizeId
                    WHERE pv.ProductId = p.ProductId
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS SizeName
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
                StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                ImageUrl = reader["ImageUrl"] == DBNull.Value ? null : reader["ImageUrl"].ToString(),
                ColorName = reader["ColorName"]?.ToString(),
                SizeName = reader["SizeName"]?.ToString()
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
                p.StockQuantity,
                (
                    SELECT pv.PictureUrl
                    FROM ProductVariants pv
                    WHERE pv.ProductId = p.ProductId
                        AND pv.PictureUrl IS NOT NULL
                        AND pv.PictureUrl <> ''
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS ImageUrl,
                (
                    SELECT col.Name
                    FROM ProductVariants pv
                    JOIN Colors col ON pv.ColorId = col.ColorId
                    WHERE pv.ProductId = p.ProductId
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS ColorName,
                (
                    SELECT s.Name
                    FROM ProductVariants pv
                    JOIN Sizes s ON pv.SizeId = s.SizeId
                    WHERE pv.ProductId = p.ProductId
                    ORDER BY pv.ProductVariantId
                    LIMIT 1
                ) AS SizeName
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
                StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                ImageUrl = reader["ImageUrl"] == DBNull.Value ? null : reader["ImageUrl"].ToString(),
                ColorName = reader["ColorName"]?.ToString(),
                SizeName = reader["SizeName"]?.ToString()
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

        await using var transaction = await connection.BeginTransactionAsync();

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
        command.Transaction = transaction;

        command.Parameters.AddWithValue("@name", request.Name?.Trim() ?? "");
        command.Parameters.AddWithValue("@description", request.Description?.Trim() ?? "");
        command.Parameters.AddWithValue("@brand", request.Brand?.Trim() ?? "");
        command.Parameters.AddWithValue("@basePrice", request.BasePrice);
        command.Parameters.AddWithValue("@categoryId", request.CategoryId);
        command.Parameters.AddWithValue("@isActive", request.IsActive);
        command.Parameters.AddWithValue("@stockQuantity", request.StockQuantity);

        await command.ExecuteNonQueryAsync();

        int productId = Convert.ToInt32(command.LastInsertedId);

        var colorName = request.ColorName?.Trim();
        var sizeName = request.SizeName?.Trim();

        if (!string.IsNullOrWhiteSpace(colorName) ||
            !string.IsNullOrWhiteSpace(sizeName))
        {
            var colorId = await GetOrCreateLookupId(
                connection,
                transaction,
                "Colors",
                "ColorId",
                string.IsNullOrWhiteSpace(colorName) ? "Default" : colorName);

            var sizeId = await GetOrCreateLookupId(
                connection,
                transaction,
                "Sizes",
                "SizeId",
                string.IsNullOrWhiteSpace(sizeName) ? "Default" : sizeName);

            await AddVariant(
                connection,
                transaction,
                productId,
                sizeId,
                colorId,
                request.StockQuantity);
        }

        await transaction.CommitAsync();

        return await GetById(productId);
    }

    private static async Task<int> GetOrCreateLookupId(
        MySqlConnection connection,
        MySqlTransaction transaction,
        string tableName,
        string idColumn,
        string name)
    {
        if (tableName is not ("Colors" or "Sizes"))
            throw new ArgumentException("Lookup table is not allowed.", nameof(tableName));

        if (idColumn is not ("ColorId" or "SizeId"))
            throw new ArgumentException("Lookup id column is not allowed.", nameof(idColumn));

        var trimmedName = name.Trim();

        await using var selectCommand = new MySqlCommand(
            $"SELECT `{idColumn}` FROM `{tableName}` WHERE LOWER(`Name`) = LOWER(@name) LIMIT 1",
            connection,
            transaction);
        selectCommand.Parameters.AddWithValue("@name", trimmedName);

        var existingId = await selectCommand.ExecuteScalarAsync();

        if (existingId != null)
            return Convert.ToInt32(existingId);

        await using var insertCommand = new MySqlCommand(
            $"INSERT INTO `{tableName}` (`Name`) VALUES (@name)",
            connection,
            transaction);
        insertCommand.Parameters.AddWithValue("@name", trimmedName);

        await insertCommand.ExecuteNonQueryAsync();
        return Convert.ToInt32(insertCommand.LastInsertedId);
    }

    private static async Task AddVariant(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int productId,
        int sizeId,
        int colorId,
        int stock)
    {
        const string sql = @"
            INSERT INTO ProductVariants
            (
                ProductId,
                SizeId,
                ColorId,
                PictureUrl,
                Stock,
                MinStock,
                MaxStock
            )
            VALUES
            (
                @productId,
                @sizeId,
                @colorId,
                @pictureUrl,
                @stock,
                @minStock,
                @maxStock
            )
        ";

        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@sizeId", sizeId);
        command.Parameters.AddWithValue("@colorId", colorId);
        command.Parameters.AddWithValue("@pictureUrl", "");
        command.Parameters.AddWithValue("@stock", stock);
        command.Parameters.AddWithValue("@minStock", 0);
        command.Parameters.AddWithValue("@maxStock", stock);

        await command.ExecuteNonQueryAsync();
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

        if (!request.CategoryId.HasValue ||
            !await CategoryExists(connection, request.CategoryId.Value))
        {
            return false;
        }

        await using var transaction = await connection.BeginTransactionAsync();

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
        command.Transaction = transaction;

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", request.Name);
        command.Parameters.AddWithValue("@description", request.Description);
        command.Parameters.AddWithValue("@brand", request.Brand);
        command.Parameters.AddWithValue("@basePrice", request.BasePrice!.Value);
        command.Parameters.AddWithValue("@categoryId", request.CategoryId.Value);
        command.Parameters.AddWithValue("@isActive", request.IsActive!.Value);
        command.Parameters.AddWithValue("@stockQuantity", request.StockQuantity!.Value);

        int rows = await command.ExecuteNonQueryAsync();

        if (rows == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.ColorName) ||
            !string.IsNullOrWhiteSpace(request.SizeName))
        {
            var colorId = await GetOrCreateLookupId(
                connection,
                transaction,
                "Colors",
                "ColorId",
                string.IsNullOrWhiteSpace(request.ColorName) ? "Default" : request.ColorName);

            var sizeId = await GetOrCreateLookupId(
                connection,
                transaction,
                "Sizes",
                "SizeId",
                string.IsNullOrWhiteSpace(request.SizeName) ? "Default" : request.SizeName);

            var variantId = await GetFirstVariantId(connection, transaction, id);

            if (variantId.HasValue)
            {
                await UpdateVariant(
                    connection,
                    transaction,
                    variantId.Value,
                    sizeId,
                    colorId,
                    request.StockQuantity!.Value);
            }
            else
            {
                await AddVariant(
                    connection,
                    transaction,
                    id,
                    sizeId,
                    colorId,
                    request.StockQuantity!.Value);
            }
        }

        await transaction.CommitAsync();

        return rows > 0;
    }

    private static async Task<int?> GetFirstVariantId(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int productId)
    {
        const string sql = @"
            SELECT ProductVariantId
            FROM ProductVariants
            WHERE ProductId = @productId
            ORDER BY ProductVariantId
            LIMIT 1
        ";

        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@productId", productId);

        var result = await command.ExecuteScalarAsync();

        if (result == null)
            return null;

        return Convert.ToInt32(result);
    }

    private static async Task UpdateVariant(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int variantId,
        int sizeId,
        int colorId,
        int stock)
    {
        const string sql = @"
            UPDATE ProductVariants
            SET
                SizeId = @sizeId,
                ColorId = @colorId,
                Stock = @stock,
                MaxStock = @stock
            WHERE ProductVariantId = @variantId
        ";

        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@variantId", variantId);
        command.Parameters.AddWithValue("@sizeId", sizeId);
        command.Parameters.AddWithValue("@colorId", colorId);
        command.Parameters.AddWithValue("@stock", stock);

        await command.ExecuteNonQueryAsync();
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
