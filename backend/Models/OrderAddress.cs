namespace Webshop
{
    public class OrderAddress
    {
        public int OrderId { get; set; }
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public string Country { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}