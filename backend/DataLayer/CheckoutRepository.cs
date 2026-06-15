using MySqlConnector;
using backend.Models;

namespace backend.Data;

public class CheckoutRepository : ICheckoutRepository
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

    public async Task<int?> CreateOrder(CheckoutRequest request, decimal totalPrice)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var customerId = await GetOrCreateCustomer(connection, transaction, request);

            const string orderSql = @"
                INSERT INTO Orders (CustomerId, OrderDate, TotalPrice, Status)
                VALUES (@customerId, @orderDate, @totalPrice, @status)
            ";

            await using var orderCommand = new MySqlCommand(orderSql, connection, transaction);
            orderCommand.Parameters.AddWithValue("@customerId", customerId);
            orderCommand.Parameters.AddWithValue("@orderDate", DateTime.UtcNow);
            orderCommand.Parameters.AddWithValue("@totalPrice", totalPrice);
            orderCommand.Parameters.AddWithValue("@status", 1);

            await orderCommand.ExecuteNonQueryAsync();
            var orderId = Convert.ToInt32(orderCommand.LastInsertedId);

            const string addressSql = @"
                INSERT INTO OrderAddresses (OrderId, Street, City, PostalCode, Country)
                VALUES (@orderId, @street, @city, @postalCode, @country)
            ";

            await using var addressCommand = new MySqlCommand(addressSql, connection, transaction);
            addressCommand.Parameters.AddWithValue("@orderId", orderId);
            addressCommand.Parameters.AddWithValue("@street", request.Street.Trim());
            addressCommand.Parameters.AddWithValue("@city", request.City.Trim());
            addressCommand.Parameters.AddWithValue("@postalCode", request.PostalCode.Trim());
            addressCommand.Parameters.AddWithValue("@country", request.Country.Trim());
            await addressCommand.ExecuteNonQueryAsync();

            foreach (var item in request.Items)
            {
                var variantId = await GetOrCreateVariantId(connection, transaction, item.VariantId);

                const string itemSql = @"
                    INSERT INTO OrderItems (OrderId, VariantId, Quantity, Price)
                    VALUES (@orderId, @variantId, @quantity, @price)
                ";

                await using var itemCommand = new MySqlCommand(itemSql, connection, transaction);
                itemCommand.Parameters.AddWithValue("@orderId", orderId);
                itemCommand.Parameters.AddWithValue("@variantId", variantId);
                itemCommand.Parameters.AddWithValue("@quantity", item.Quantity);
                itemCommand.Parameters.AddWithValue("@price", item.Price);
                await itemCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return orderId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<OrderSummaryDto>> GetOrders()
    {
        var orders = new List<OrderSummaryDto>();
        var orderMap = new Dictionary<int, OrderSummaryDto>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT
                o.OrderId,
                o.OrderDate,
                o.TotalPrice,
                o.Status,
                c.FirstName,
                c.LastName,
                c.Email,
                oa.Street,
                oa.City,
                oa.PostalCode,
                oa.Country,
                oi.OrderItemId,
                oi.Quantity,
                oi.Price,
                p.Name AS ProductName
            FROM Orders o
            JOIN Customer c ON o.CustomerId = c.CustomerId
            LEFT JOIN OrderAddresses oa ON o.OrderId = oa.OrderId
            LEFT JOIN OrderItems oi ON o.OrderId = oi.OrderId
            LEFT JOIN ProductVariants pv ON oi.VariantId = pv.ProductVariantId
            LEFT JOIN Products p ON pv.ProductId = p.ProductId
            ORDER BY o.OrderDate DESC, o.OrderId DESC, oi.OrderItemId
        ";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var orderId = Convert.ToInt32(reader["OrderId"]);

            if (!orderMap.TryGetValue(orderId, out var order))
            {
                order = new OrderSummaryDto
                {
                    OrderId = orderId,
                    OrderDate = Convert.ToDateTime(reader["OrderDate"]),
                    TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                    Status = GetStatusName(Convert.ToInt32(reader["Status"])),
                    CustomerName = $"{reader["FirstName"]} {reader["LastName"]}".Trim(),
                    Email = reader["Email"]?.ToString() ?? "",
                    Street = reader["Street"]?.ToString() ?? "",
                    City = reader["City"]?.ToString() ?? "",
                    PostalCode = reader["PostalCode"]?.ToString() ?? "",
                    Country = reader["Country"]?.ToString() ?? ""
                };

                orderMap.Add(orderId, order);
                orders.Add(order);
            }

            if (reader["OrderItemId"] != DBNull.Value)
            {
                order.Items.Add(new OrderItemSummaryDto
                {
                    Name = reader["ProductName"]?.ToString() ?? "Unknown product",
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Price = Convert.ToDecimal(reader["Price"])
                });
            }
        }

        return orders;
    }

    private static async Task<int> GetOrCreateCustomer(
        MySqlConnection connection,
        MySqlTransaction transaction,
        CheckoutRequest request)
    {
        const string selectSql = @"
            SELECT CustomerId
            FROM Customer
            WHERE Email = @email
            LIMIT 1
        ";

        await using var selectCommand = new MySqlCommand(selectSql, connection, transaction);
        selectCommand.Parameters.AddWithValue("@email", request.Email.Trim());

        var existingId = await selectCommand.ExecuteScalarAsync();

        if (existingId != null)
            return Convert.ToInt32(existingId);

        const string insertSql = @"
            INSERT INTO Customer (FirstName, LastName, Email, PasswordHash, Phone, IsMember, CreatedAt)
            VALUES (@firstName, @lastName, @email, NULL, NULL, 0, @createdAt)
        ";

        await using var insertCommand = new MySqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@firstName", request.FirstName.Trim());
        insertCommand.Parameters.AddWithValue("@lastName", request.LastName.Trim());
        insertCommand.Parameters.AddWithValue("@email", request.Email.Trim());
        insertCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

        await insertCommand.ExecuteNonQueryAsync();
        return Convert.ToInt32(insertCommand.LastInsertedId);
    }

    private static async Task<int> GetOrCreateVariantId(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int productId)
    {
        const string selectSql = @"
            SELECT ProductVariantId
            FROM ProductVariants
            WHERE ProductId = @productId
            ORDER BY ProductVariantId
            LIMIT 1
        ";

        await using var selectCommand = new MySqlCommand(selectSql, connection, transaction);
        selectCommand.Parameters.AddWithValue("@productId", productId);

        var existingId = await selectCommand.ExecuteScalarAsync();

        if (existingId != null)
            return Convert.ToInt32(existingId);

        var colorId = await GetOrCreateLookupId(connection, transaction, "Colors", "ColorId", "Default");
        var sizeId = await GetOrCreateLookupId(connection, transaction, "Sizes", "SizeId", "Default");

        const string productSql = @"
            SELECT StockQuantity
            FROM Products
            WHERE ProductId = @productId
            LIMIT 1
        ";

        await using var productCommand = new MySqlCommand(productSql, connection, transaction);
        productCommand.Parameters.AddWithValue("@productId", productId);
        var stockResult = await productCommand.ExecuteScalarAsync();
        var stock = stockResult == null ? 0 : Convert.ToInt32(stockResult);

        const string insertSql = @"
            INSERT INTO ProductVariants (ProductId, SizeId, ColorId, PictureUrl, Stock, MinStock, MaxStock)
            VALUES (@productId, @sizeId, @colorId, '', @stock, 0, @stock)
        ";

        await using var insertCommand = new MySqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@productId", productId);
        insertCommand.Parameters.AddWithValue("@sizeId", sizeId);
        insertCommand.Parameters.AddWithValue("@colorId", colorId);
        insertCommand.Parameters.AddWithValue("@stock", stock);

        await insertCommand.ExecuteNonQueryAsync();
        return Convert.ToInt32(insertCommand.LastInsertedId);
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

        await using var selectCommand = new MySqlCommand(
            $"SELECT `{idColumn}` FROM `{tableName}` WHERE LOWER(`Name`) = LOWER(@name) LIMIT 1",
            connection,
            transaction);
        selectCommand.Parameters.AddWithValue("@name", name);

        var existingId = await selectCommand.ExecuteScalarAsync();

        if (existingId != null)
            return Convert.ToInt32(existingId);

        await using var insertCommand = new MySqlCommand(
            $"INSERT INTO `{tableName}` (`Name`) VALUES (@name)",
            connection,
            transaction);
        insertCommand.Parameters.AddWithValue("@name", name);

        await insertCommand.ExecuteNonQueryAsync();
        return Convert.ToInt32(insertCommand.LastInsertedId);
    }

    private static string GetStatusName(int status)
    {
        return status switch
        {
            0 => "Pending",
            1 => "Paid",
            2 => "Shipped",
            3 => "Cancelled",
            4 => "Completed",
            _ => "Unknown"
        };
    }

}
