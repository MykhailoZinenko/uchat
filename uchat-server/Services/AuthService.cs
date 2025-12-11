using Microsoft.Extensions.Logging;
using uchat_common.Dtos;
using uchat_common.Enums;
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
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberService _roomMemberService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserService userService,
        IHashService hashService,
        ICryptographyService cryptographyService,
        ISessionService sessionService,
        IRoomRepository roomRepository,
        IRoomMemberService roomMemberService,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _hashService = hashService;
        _cryptographyService = cryptographyService;
        _sessionService = sessionService;
        _roomRepository = roomRepository;
        _roomMemberService = roomMemberService;
        _logger = logger;
    }

    public async Task<AuthDto> RegisterAsync(string username, string password, string deviceInfo, string? ipAddress = null, string? email = null)
    {
        _logger.LogInformation("AuthService.Register start Username={Username} Device={Device} Ip={Ip}", username, deviceInfo, ipAddress);

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

        await EnsureGlobalMembershipAsync(createdUser.Id, createdUser.Username);

        _logger.LogInformation("AuthService.Register success Username={Username} SessionId={SessionId}", username, session.Id);

        return new AuthDto
        {
            SessionToken = sessionToken,
            UserId = createdUser.Id,
            Username = createdUser.Username
        };
    }

    public async Task<AuthDto> LoginAsync(string username, string password, string deviceInfo, string? ipAddress = null)
    {
        _logger.LogInformation("AuthService.Login start Username={Username} Device={Device} Ip={Ip}", username, deviceInfo, ipAddress);

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

        _logger.LogInformation("AuthService.Login success Username={Username} SessionId={SessionId}", username, session.Id);

        return new AuthDto
        {
            SessionToken = sessionToken,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<AuthDto> LoginWithRefreshTokenAsync(string sessionToken, string? deviceInfo = null, string? ipAddress = null)
    {
        _logger.LogInformation("AuthService.LoginWithRefreshToken start");

        var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
        var user = await _userService.GetUserByIdAsync(session.UserId);

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

        _logger.LogInformation("AuthService.LoginWithRefreshToken success SessionId={SessionId}", session.Id);

        return new AuthDto
        {
            SessionToken = sessionToken,
            UserId = user?.Id ?? session.UserId,
            Username = user?.Username ?? string.Empty
        };
    }

    private async Task EnsureGlobalMembershipAsync(int userId, string username)
    {
        var globalRoomId = await _roomRepository.GetGlobalRoomIdAsync();
        if (globalRoomId == null)
        {
            var globalRoom = new Room
            {
                RoomType = RoomType.Global,
                RoomName = "Global Chat",
                RoomDescription = "Welcome to uchat! This is the global chat room where everyone can communicate.",
                IsGlobal = true,
                CreatedByUserId = null
            };

            var created = await _roomRepository.CreateAsync(globalRoom);
            globalRoomId = created.Id;
            _logger.LogInformation("Created missing global room with Id={RoomId}", globalRoomId);
        }

        await _roomMemberService.JoinRoomAsync(globalRoomId.Value, userId);
        _logger.LogInformation("User added to global room. UserId={UserId} Username={Username} RoomId={RoomId}", userId, username, globalRoomId);
    }
}
