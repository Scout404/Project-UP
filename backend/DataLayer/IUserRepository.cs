using backend.Models;

namespace backend.Data;

public interface IUserRepository
{
    Task<User?> GetByUsername(string username);
    Task<User?> GetByEmail(string email);
    Task<User?> GetById(int id);
    Task<int> Create(User user);
}
