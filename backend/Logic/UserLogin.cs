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

    public async Task<User?> Authenticate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _users.GetByUsername(username);

        if (user == null)
            return null;

        bool validPassword =
            BCrypt.Net.BCrypt.Verify(password, user.Password);

        if (!validPassword)
            return null;

        return user;
    }
}