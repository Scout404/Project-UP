using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace backend.Logic;
public class CheckoutService
{
    private readonly AppDbContext _db;

    public CheckoutService(AppDbContext conn)
    {
        _db = conn;
    }

    public async Task<Orders> Checkout(int userId, CheckoutRequest request)
    {
        // load cart items 
        var usersCart = await _db.Carts.Include(x=> x.Items).FirstOrDefaultAsync(x=> x.UserId == userId );

        // var customer = userId;
        if(usersCart == null || usersCart.Items.Count == 0)
        {
            throw new ArgumentException("Empty cart");
        }


        var usersOrder = new Orders
        {
            CustomerId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Paid,
            TotalPrice = usersCart.Items.Sum(x=> x.Price * x.Quantity),


        };

        foreach(var item in usersCart.Items)
        {
            usersOrder.Items.Add(
                new OrderItem
                {
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    Price = item.Price,

                }
            );
        }

        // save shipping address
        usersOrder.OrderAddress = new OrderAddress
        {
            Street =  request.Street,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country

        };

        _db.Orders.Add(usersOrder);
        await _db.SaveChangesAsync();

        //remove after purchase 
        _db.CartItems.RemoveRange(usersCart.Items);
        await _db.SaveChangesAsync();

        TxtAsConfirmationEmail(usersOrder, request);

        return usersOrder;

    }


    private void TxtAsConfirmationEmail(Orders order, CheckoutRequest request)
    {
        var confirmation = new StringBuilder();

        confirmation.AppendLine("ORDER CONFIRMATION");
        confirmation.AppendLine("----------------------");
        confirmation.AppendLine($"Order ID: {order.OrderId}");
        confirmation.AppendLine($"Customer ID: {order.CustomerId}");
        confirmation.AppendLine($"Order Date: {order.OrderDate}");
        confirmation.AppendLine($"Total Price: €{order.TotalPrice}");
        confirmation.AppendLine();

        confirmation.AppendLine("Shipping Address");
        confirmation.AppendLine(request.Street);
        confirmation.AppendLine(request.City);
        confirmation.AppendLine(request.PostalCode);
        confirmation.AppendLine(request.Country);
        confirmation.AppendLine();

        confirmation.AppendLine("Products");

        foreach (var item in order.Items)
        {
            confirmation.AppendLine(
                $"Variant {item.VariantId} | Qty: {item.Quantity} | €{item.Price}"
            );
        }

        var path = Path.Combine(Directory.GetCurrentDirectory(), $"Order_{order.OrderId}.txt");
        File.WriteAllText(path, confirmation.ToString());

    }







}