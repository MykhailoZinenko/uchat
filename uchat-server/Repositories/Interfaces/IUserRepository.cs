using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByUsernameAndPasswordAsync(string username, string passwordHash);
    Task<List<User>> SearchByUsernameAsync(string query, int limit = 20);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<User> DeleteAsync(User user);
}
