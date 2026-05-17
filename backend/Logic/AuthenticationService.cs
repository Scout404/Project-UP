using backend.Data;
using backend.Models;

namespace backend.Logic;

public class AuthenticationService
{
    private readonly UserRepository _users;

    public AuthenticationService(UserRepository users)
    {
        _users = users;
    }

    public async Task<User?> Authenticate(
        string username,
        string password
    )
    {
        var user =
            await _users.GetByUsername(username);

        if (user == null)
            return null;

        bool validPassword =
            BCrypt.Net.BCrypt.Verify(
                password,
                user.Password
            );

        if (!validPassword)
            return null;

        return user;
    }

    public async Task<(bool Success, string Error)>
        Register(
            string username,
            string email,
            string password
        )
    {
        var existingUser =
            await _users.GetByUsername(username);

        if (existingUser != null)
        {
            return (false, "Username already exists");
        }

        var existingEmail =
            await _users.GetByEmail(email);

        if (existingEmail != null)
        {
            return (false, "Email already exists");
        }

        var user = new User
        {
            Username = username,
            Email = email,
            Password =
                BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        user.Id = await _users.Create(user);

        return (true, "");
    }
}