using uchat_common.Dtos;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IHashService _hashService;
    private readonly ICryptographyService _cryptographyService;
    private readonly ISessionService _sessionService;

    public AuthService(
        IUserService userService,
        IHashService hashService,
        ICryptographyService cryptographyService,
        ISessionService sessionService)
    {
        _userService = userService;
        _hashService = hashService;
        _cryptographyService = cryptographyService;
        _sessionService = sessionService;
    }

    public async Task<AuthDto> RegisterAsync(string username, string password, string deviceInfo, string? ipAddress = null, string? email = null)
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

        var createdUser = await _userService.CreateUserAsync(user);

        var sessionToken = _cryptographyService.GenerateSessionToken();

        var session = new Session
        {
            SessionToken = sessionToken,
            UserId = createdUser.Id,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_cryptographyService.GetSessionLifetimeMs()),
            LastActivityAt = DateTime.UtcNow
        };

        await _sessionService.CreateSessionAsync(session);

        return new AuthDto
        {
            SessionToken = sessionToken
        };
    }

    public async Task<AuthDto> LoginAsync(string username, string password, string deviceInfo, string? ipAddress = null)
    {
        var user = await _userService.GetUserByUsernameAsync(username);
        if (user == null)
        {
            throw new AppException("User not found or invalid password");
        }

        if (!_hashService.Verify(password, user.PasswordHash))
        {
            throw new AppException("User not found or invalid password");
        }

        var sessionToken = _cryptographyService.GenerateSessionToken();

        var session = new Session
        {
            SessionToken = sessionToken,
            UserId = user.Id,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_cryptographyService.GetSessionLifetimeMs()),
            LastActivityAt = DateTime.UtcNow
        };

        await _sessionService.CreateSessionAsync(session);

        return new AuthDto
        {
            SessionToken = sessionToken
        };
    }

    public async Task<AuthDto> LoginWithRefreshTokenAsync(string sessionToken, string? deviceInfo = null, string? ipAddress = null)
    {
        var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
        
        if (deviceInfo != null)
        {
            session.DeviceInfo = deviceInfo;
            await _sessionService.UpdateSessionAsync(session);
        }
        
        if (ipAddress != null)
        {
            session.IpAddress = ipAddress;
            await _sessionService.UpdateSessionAsync(session);
        }

        return new AuthDto
        {
            SessionToken = sessionToken
        };
    }
}
