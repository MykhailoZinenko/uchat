using uchat_server.Data.Entities;
using uchat_server.Exceptions;

namespace uchat_server.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IHashService _hashService;

    public AuthService(IUserService userService, IHashService hashService)
    {
        _userService = userService;
        _hashService = hashService;
    }

    public async Task<User> RegisterAsync(string username, string password, string? email = null)
    {
        var passwordHash = _hashService.Hash(password);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsOnline = false
        };

        return await _userService.CreateUserAsync(user);
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _userService.GetUserByUsernameAsync(username);
        if (user == null)
        {
            return null;
        }

        if (!_hashService.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }
}
