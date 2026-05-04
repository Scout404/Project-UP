public class OrderAddress
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public Orders Order { get; set; } = null!;
}
