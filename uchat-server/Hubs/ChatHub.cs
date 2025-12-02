using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using uchat_server.Services;
using uchat_common.Dtos;
using uchat_server.Exceptions;

namespace uchat_server.Hubs;

public class ChatHub : Hub
{
    private readonly AuthService _authService;
    private readonly ChatService _chatService;
    private readonly IMapperService _mapperService;
    private static readonly ConcurrentDictionary<string, (int UserId, string Username)> _connectedUsers = new();
    private static readonly ConcurrentDictionary<string, string> _connectionToSession = new();

    public ChatHub(AuthService authService, ChatService chatService, IMapperService mapperService)
    {
        _authService = authService;
        _chatService = chatService;
        _mapperService = mapperService;
    }

    public async Task<ApiResponse<UserDto>> Register(string username, string password, string deviceInfo = "Unknown")
    {
        try
        {
            var user = await _authService.RegisterAsync(username, password);
            
            _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
            await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
            await _chatService.UpdateUserLastSeenAsync(user.Id);

            var session = await _chatService.CreateSessionAsync(user.Id, deviceInfo);
            _connectionToSession[Context.ConnectionId] = session.Token;

            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Account created successfully",
                Data = _mapperService.MapToUserDto(user)
            };
        }
        catch (AppException ex) 
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            };

        }
        catch (Exception)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public async Task<ApiResponse<UserDto>> Login(string username, string password, string deviceInfo = "Unknown")
    {
        try
        {
            var user = await _authService.LoginAsync(username, password);

            if (user == null)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found or invalid password"
                };
            }

            _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
            await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
            await _chatService.UpdateUserLastSeenAsync(user.Id);

            var newSession = await _chatService.CreateSessionAsync(user.Id, deviceInfo);
            _connectionToSession[Context.ConnectionId] = newSession.Token;

            return new ApiResponse<UserDto>
            {
                Success = true,
                Message = "Logged in successfully",
                Data = _mapperService.MapToUserDto(user)
            };
        }
        catch (AppException ex) 
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            };

        }
        catch (Exception)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public async Task<ApiResponse<UserDto>> LoginWithSession(string sessionToken)
    {
        var (isValid, user) = await _chatService.ValidateSessionAsync(sessionToken);

        if (!isValid || user == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Invalid or expired session"
            };
        }

        _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
        _connectionToSession[Context.ConnectionId] = sessionToken;
        await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
        await _chatService.UpdateUserLastSeenAsync(user.Id);

        return new ApiResponse<UserDto>
        {
            Success = true,
            Message = "Logged in with session successfully",
            Data = _mapperService.MapToUserDto(user)
        };
    }

    public async Task Logout()
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var userInfo))
        {
            if (_connectionToSession.TryRemove(Context.ConnectionId, out var sessionToken))
            {
                await _chatService.RevokeSessionAsync(sessionToken);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "LoggedIn");
            await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
        }
    }

    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessions()
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var userInfo))
        {
            return new ApiResponse<List<SessionInfo>>
            {
                Success = false,
                Message = "Not logged in"
            };
        }

        var sessions = await _chatService.GetUserSessionsAsync(userInfo.UserId);
        var sessionInfos = sessions.Select(s => new SessionInfo
        {
            Token = s.Token,
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

    public async Task<ApiResponse<bool>> RevokeSession(string sessionToken)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out _))
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Not logged in"
            };
        }

        var success = await _chatService.RevokeSessionAsync(sessionToken);

        if (success)
        {
            var connectionWithSession = _connectionToSession.FirstOrDefault(kvp => kvp.Value == sessionToken);

            if (!connectionWithSession.Equals(default(KeyValuePair<string, string>)))
            {
                var connectionId = connectionWithSession.Key;
                await Clients.Client(connectionId).SendAsync("SessionRevoked", "Your session has been revoked");

                _connectedUsers.TryRemove(connectionId, out var userInfo);
                _connectionToSession.TryRemove(connectionId, out _);
                await Groups.RemoveFromGroupAsync(connectionId, "LoggedIn");

                if (userInfo.UserId > 0)
                {
                    await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
                }
            }
        }

        return new ApiResponse<bool>
        {
            Success = success,
            Message = success ? "Session revoked" : "Failed to revoke session",
            Data = success
        };
    }

    public async Task SendMessage(string message)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var userInfo))
        {
            await Clients.Caller.SendAsync("Error", "You must login first");
            return;
        }

        var savedMessage = await _chatService.SaveMessageAsync(userInfo.UserId, message);

        var messageDto = new MessageDto
        {
            ConnectionId = Context.ConnectionId,
            Username = userInfo.Username,
            Content = message,
            SentAt = savedMessage.SentAt
        };

        await Clients.Group("LoggedIn").SendAsync("ReceiveMessage", messageDto);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var userInfo))
        {
            _connectionToSession.TryRemove(Context.ConnectionId, out _);
            await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
