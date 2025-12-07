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
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IErrorMapper _errorMapper;

    public ChatHub(
        IAuthService authService, 
        ChatService chatService, 
        ISessionService sessionService, 
        IJwtService jwtService,
        IUserService userService,
        IErrorMapper errorMapper)
    {
        _authService = authService;
        _chatService = chatService;
        _sessionService = sessionService;
        _jwtService = jwtService;
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

    public async Task<ApiResponse<string>> RefreshAccessToken(string refreshToken)
    {
        try
        {
            var payload = await _jwtService.ValidateRefreshTokenAsync(refreshToken);
            var newAccessToken = _jwtService.GenerateAccessToken(new Models.AccessTokenPayload(payload.UserId, payload.SessionId));

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Access token refreshed successfully",
                Data = newAccessToken
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<string>(ex);
        }
    }

    public async Task<ApiResponse<bool>> Logout(string accessToken)
    {
        try
        {
            var payload = await _jwtService.ValidateAccessTokenAsync(accessToken);
            var success = await _sessionService.RevokeSessionAsync(payload.SessionId);

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

    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessions(string accessToken)
    {
        try
        {
            var payload = await _jwtService.ValidateAccessTokenAsync(accessToken);
            var sessions = await _sessionService.GetActiveSessionsByUserIdAsync(payload.UserId);
            var sessionInfos = sessions.Select(s => new SessionInfo
            {
                Token = s.RefreshToken,
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

    public async Task<ApiResponse<bool>> RevokeSession(string accessToken, int sessionId)
    {
        try
        {
            var payload = await _jwtService.ValidateAccessTokenAsync(accessToken);
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

    public async Task SendMessage(string accessToken, string message)
    {
        try
        {
            var payload = await _jwtService.ValidateAccessTokenAsync(accessToken);
            var user = await _userService.GetUserByIdAsync(payload.UserId);
            
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
