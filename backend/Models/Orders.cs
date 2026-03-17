public enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Cancelled,
    Completed
}

public class Orders
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; }

    public Customer Customer { get; set; } = null!;
    public OrderAddress OrderAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}