using backend.Models;

namespace backend.Data;

public interface ICheckoutRepository
{
    Task<int?> GetProductStock(int productId);
    Task<bool> ReduceStock(int productId, int quantity);
    Task<int?> CreateOrder(CheckoutRequest request, decimal totalPrice);
    Task<List<OrderSummaryDto>> GetOrders();
}
