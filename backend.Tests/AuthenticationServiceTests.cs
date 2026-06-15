using backend.Data;
using backend.Logic;
using backend.Models;

namespace backend.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task Register_HashesPasswordAndStoresCustomerRole()
    {
        var users = new FakeUserRepository();
        var service = new AuthenticationService(users);

        var result = await service.Register("marktester", "mark.tester@example.com", "Secret123!");

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal(77, result.User.Id);
        Assert.Equal("Customer", result.User.Role);
        Assert.NotEqual("Secret123!", users.CreatedUser!.Password);
        Assert.StartsWith("$2", users.CreatedUser.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secret123!", users.CreatedUser.Password));
    }

    [Fact]
    public async Task Authenticate_ReturnsNullWhenBcryptPasswordDoesNotMatch()
    {
        var users = new FakeUserRepository();
        users.UsersByUsername["admin"] = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@shop.com",
            Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        var service = new AuthenticationService(users);

        var result = await service.Authenticate("admin", "wrong-password");

        Assert.Null(result);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<string, User> UsersByUsername { get; } = new();
        public Dictionary<string, User> UsersByEmail { get; } = new();
        public User? CreatedUser { get; private set; }

        public Task<User?> GetByUsername(string username)
        {
            UsersByUsername.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmail(string email)
        {
            UsersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetById(int id)
        {
            var user = UsersByUsername.Values.FirstOrDefault(current => current.Id == id);
            return Task.FromResult(user);
        }

        public Task<int> Create(User user)
        {
            CreatedUser = user;
            UsersByUsername[user.Username] = user;
            UsersByEmail[user.Email] = user;
            return Task.FromResult(77);
        }
    }
}
