namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; } // "Admin" or "Customer"
    public DateTime CreatedAt { get; set; }
}
