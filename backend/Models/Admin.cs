namespace Webshop
{
    public class Admin
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public bool IsAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }
}