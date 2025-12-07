using uchat_common.Dtos;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Models;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly IHashService _hashService;
    private readonly IJwtService _jwtService;
    private readonly ISessionService _sessionService;

    public AuthService(
        IUserService userService,
        IHashService hashService,
        IJwtService jwtService,
        ISessionService sessionService)
    {
        _userService = userService;
        _hashService = hashService;
        _jwtService = jwtService;
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

        var session = new Session
        {
            RefreshToken = string.Empty,
            UserId = createdUser.Id,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_jwtService.GetRefreshTokenLifetimeMs()),
            LastActivityAt = DateTime.UtcNow
        };

        var createdSession = await _sessionService.CreateSessionAsync(session);

        var accessToken = _jwtService.GenerateAccessToken(new AccessTokenPayload(createdUser.Id, createdSession.Id));
        var refreshToken = _jwtService.GenerateRefreshToken(new RefreshTokenPayload(createdUser.Id, createdSession.Id));

        createdSession.RefreshToken = refreshToken;
        await _sessionService.UpdateSessionAsync(createdSession);

        return new AuthDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
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

        var session = new Session
        {
            RefreshToken = string.Empty,
            UserId = user.Id,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMilliseconds(_jwtService.GetRefreshTokenLifetimeMs()),
            LastActivityAt = DateTime.UtcNow
        };

        var createdSession = await _sessionService.CreateSessionAsync(session);

        var accessToken = _jwtService.GenerateAccessToken(new AccessTokenPayload(user.Id, createdSession.Id));
        var refreshToken = _jwtService.GenerateRefreshToken(new RefreshTokenPayload(user.Id, createdSession.Id));

        createdSession.RefreshToken = refreshToken;
        await _sessionService.UpdateSessionAsync(createdSession);

        return new AuthDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthDto> LoginWithRefreshTokenAsync(string refreshToken, string? deviceInfo = null, string? ipAddress = null)
    {
        RefreshTokenPayload payload = await _jwtService.ValidateRefreshTokenAsync(refreshToken);

        Session session = await _sessionService.GetSessionByIdAsync(payload.SessionId);

        string newAccessToken = _jwtService.GenerateAccessToken(new AccessTokenPayload(session.UserId, session.Id));
        string newRefreshToken = _jwtService.GenerateRefreshToken(new RefreshTokenPayload(session.UserId, session.Id));

        session.RefreshToken = newRefreshToken;
        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddMilliseconds(_jwtService.GetRefreshTokenLifetimeMs());
        
        if (deviceInfo != null)
        {
            session.DeviceInfo = deviceInfo;
        }
        
        if (ipAddress != null)
        {
            session.IpAddress = ipAddress;
        }

        await _sessionService.UpdateSessionAsync(session);

        return new AuthDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}
