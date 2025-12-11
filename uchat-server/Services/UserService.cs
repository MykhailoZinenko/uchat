using Microsoft.Extensions.Logging;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IHashService _hashService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IHashService hashService, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _logger.LogDebug("UserService.CreateUser start Username={Username}", user.Username);
        var existingUser = await _userRepository.GetByUsernameAsync(user.Username);
        if (existingUser != null)
        {
            _logger.LogWarning("UserService.CreateUser duplicate Username={Username}", user.Username);
            throw new AppException("Username already exists");
        }

        var created = await _userRepository.CreateAsync(user);
        _logger.LogInformation("UserService.CreateUser success UserId={UserId} Username={Username}", created.Id, created.Username);
        return created;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        return await _userRepository.UpdateAsync(user);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    public async Task<List<User>> SearchUsersAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<User>();
        }

        return await _userRepository.SearchByUsernameAsync(query, limit);
    }

    public async Task<User> SetUserOfflineAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AppException("User not found");
        }

        user.IsOnline = false;
        user.LastSeenAt = DateTime.UtcNow;

        return await _userRepository.UpdateAsync(user);
    }

    public async Task<User> SetUserOnlineAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AppException("User not found");
        }

        user.IsOnline = true;
        user.LastSeenAt = null;

        return await _userRepository.UpdateAsync(user);
    }
}
