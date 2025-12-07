using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> SetUserOfflineAsync(int userId);
    Task<User> SetUserOnlineAsync(int userId);
}
