using Microsoft.AspNetCore.SignalR;
using uchat_server.Services;
using uchat_common.Dtos;
using uchat_server.Exceptions;

namespace uchat_server.Hubs;

public class ChatHub : Hub
{
    private readonly IAuthService _authService;
    private readonly ChatService _chatService;
    private readonly ISessionService _sessionService;
    private readonly ICryptographyService _cryptographyService;
    private readonly IUserService _userService;
    private readonly IErrorMapper _errorMapper;

    public ChatHub(
        IAuthService authService, 
        ChatService chatService, 
        ISessionService sessionService, 
        ICryptographyService cryptographyService,
        IUserService userService,
        IErrorMapper errorMapper)
    {
        _authService = authService;
        _chatService = chatService;
        _sessionService = sessionService;
        _cryptographyService = cryptographyService;
        _userService = userService;
        _errorMapper = errorMapper;
    }

    public async Task<ApiResponse<AuthDto>> Register(string username, string password, string? deviceInfo = null, string? ipAddress = null)
    {
        try
        {
            string finalDeviceInfo = deviceInfo ?? "Unknown";
            string finalIpAddress = ipAddress ?? Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            AuthDto authDto = await _authService.RegisterAsync(username, password, finalDeviceInfo, finalIpAddress);
            
            return new ApiResponse<AuthDto>
            {
                Success = true,
                Message = "Account created successfully",
                Data = authDto
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<AuthDto>(ex);
        }
    }

    public async Task<ApiResponse<AuthDto>> Login(string username, string password, string? deviceInfo = null, string? ipAddress = null)
    {
        try
        {
            string finalDeviceInfo = deviceInfo ?? "Unknown";
            string finalIpAddress = ipAddress ?? Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            AuthDto authDto = await _authService.LoginAsync(username, password, finalDeviceInfo, finalIpAddress);

            return new ApiResponse<AuthDto>
            {
                Success = true,
                Message = "Logged in successfully",
                Data = authDto
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<AuthDto>(ex);
        }
    }

    public async Task<ApiResponse<AuthDto>> LoginWithRefreshToken(string refreshToken, string? deviceInfo = null, string? ipAddress = null)
    {
        try
        {
            AuthDto authDto = await _authService.LoginWithRefreshTokenAsync(refreshToken, deviceInfo, ipAddress);
            if (authDto == null)
            {
                return new ApiResponse<AuthDto>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token"
                };
            }

            return new ApiResponse<AuthDto>
            {
                Success = true,
                Message = "Tokens refreshed successfully",
                Data = authDto
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<AuthDto>(ex);
        }
    }

    public async Task<ApiResponse<bool>> Logout(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var success = await _sessionService.RevokeSessionAsync(session.Id);

            return new ApiResponse<bool>
            {
                Success = success,
                Message = success ? "Logged out successfully" : "Failed to logout",
                Data = success
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessions(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var sessions = await _sessionService.GetActiveSessionsByUserIdAsync(session.UserId);
            var sessionInfos = sessions.Select(s => new SessionInfo
            {
                Token = s.SessionToken,
                DeviceInfo = s.DeviceInfo,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                ExpiresAt = s.ExpiresAt
            }).ToList();

            return new ApiResponse<List<SessionInfo>>
            {
                Success = true,
                Data = sessionInfos
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<SessionInfo>>(ex);
        }
    }

    public async Task<ApiResponse<bool>> RevokeSession(string sessionToken, int sessionId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var success = await _sessionService.RevokeSessionAsync(sessionId);

            return new ApiResponse<bool>
            {
                Success = success,
                Message = success ? "Session revoked" : "Failed to revoke session",
                Data = success
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task SendMessage(string sessionToken, string message)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var user = await _userService.GetUserByIdAsync(session.UserId);
            
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }

            var savedMessage = await _chatService.SaveMessageAsync(user.Id, message);

            var messageDto = new MessageDto
            {
                ConnectionId = Context.ConnectionId,
                Username = user.Username,
                Content = message,
                SentAt = savedMessage.SentAt
            };

            await Clients.Group("LoggedIn").SendAsync("ReceiveMessage", messageDto);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
