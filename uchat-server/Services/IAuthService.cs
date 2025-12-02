using uchat_server.Data.Entities;
using uchat_server.Exceptions;

namespace uchat_server.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string password, string? email = null);
    Task<User?> LoginAsync(string username, string password);
}
