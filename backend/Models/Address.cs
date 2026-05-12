public class Address
{
    public int AddressId { get; set; }
    public int CustomerId { get; set; }

    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}