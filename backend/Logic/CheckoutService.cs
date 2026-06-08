using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace backend.Logic;
public class CheckoutService
{

    private readonly CheckoutRepository _repository;

    public CheckoutService(CheckoutRepository repo)
    {
        _repository = repo;
    }

    public async Task<(bool Success, byte[]? Receipt, string? ErrorMessage)> Checkout(CheckoutRequest request)
    {
        //validate
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email) ||
            !request.Email.Contains("@") ||
            string.IsNullOrWhiteSpace(request.Street) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.PostalCode) ||
            string.IsNullOrWhiteSpace(request.Country) ||
            string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            return (false, null, "invalid customer info");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return (false, null, "Cart is empty");
        }

        var sb = new StringBuilder();

        sb.AppendLine("===== ORDER RECEIPT =====");
        sb.AppendLine("Thank you for your order! As soon as your package is on the way you will recieve more info on the delivery date.");
        sb.AppendLine($"Order date: {DateTime.UtcNow}");
        sb.AppendLine();
        sb.AppendLine("Delivery address:");
        sb.AppendLine($"{request.FirstName} {request.LastName}");
        sb.AppendLine(request.Email);
        sb.AppendLine($"{request.Street} {request.PostalCode}, {request.City} ");
        sb.AppendLine();
        sb.AppendLine("Your order: ");

        decimal total = 0;

        foreach (var item in request.Items)
        {
            var stock = await _repository.GetProductStock(item.VariantId);
   
            if (stock is null )
            {

                return (false, null, "Product not found");
            }

            if(stock < item.Quantity)
            {
                return (false,null, $"Not enough stock for: {item.Name}");
            }

            // reduce stock in db
            await _repository.ReduceStock(item.VariantId, item.Quantity);

            // reciept
            sb.AppendLine($"{item.Name} x{item.Quantity} - €{item.Price * item.Quantity}");

            total += item.Price * item.Quantity;
        }

        sb.AppendLine();
        sb.AppendLine($"TOTAL: €{total}");
        sb.AppendLine($"payment type: {request.PaymentMethod}");

        return (true, Encoding.UTF8.GetBytes(sb.ToString()), null);
    }
    private byte[] CreateReceiptText(CheckoutRequest request, int productId, int quantity)
    {
        var sb = new StringBuilder();

        sb.AppendLine("===== YOUR ORDER RECEIPT =====");
        sb.AppendLine($"Date: {DateTime.UtcNow}");
        sb.AppendLine();
        sb.AppendLine($"ProductId: {productId}");
        sb.AppendLine($"Quantity: {quantity}");
        sb.AppendLine();
        sb.AppendLine("CUSTOMER:");
        sb.AppendLine($"{request.FirstName} {request.LastName}");
        sb.AppendLine(request.Email);
        sb.AppendLine($"{request.Street}, {request.City}, {request.PostalCode}, {request.Country}");
        sb.AppendLine();
        sb.AppendLine($"Payment: {request.PaymentMethod}");
        sb.AppendLine("=========================");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}