using backend.Models;
using System.Text.Json;

namespace backend.Logic;
public class AuthenticationService
{
    private readonly string _usersJsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "users.json");

    public User? Authenticate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        try
        {
            if (!File.Exists(_usersJsonPath))
                return null;

            string jsonContent = File.ReadAllText(_usersJsonPath);
            var users = JsonSerializer.Deserialize<List<User>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (users == null)
                return null;

            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during authentication: {ex.Message}");
            return null;
        }
    }
}
