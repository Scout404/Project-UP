public class Review
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public string ReviewText { get; set; } = null!;
    public int Rating { get; set; }

    public Product Product { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}