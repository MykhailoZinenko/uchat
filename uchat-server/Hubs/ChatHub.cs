using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using uchat_server.Services;
using uchat_common.Dtos;

namespace uchat_server.Hubs;

public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private static readonly ConcurrentDictionary<string, (int UserId, string Username)> _connectedUsers = new();
    private static readonly ConcurrentDictionary<string, string> _connectionToSession = new(); // ConnectionId -> SessionToken

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<LoginResult> Login(string username, string password, string deviceInfo = "Unknown")
    {
        var user = await _chatService.GetUserByUsernameAsync(username);

        if (user == null)
        {
            user = await _chatService.CreateUserAsync(username, password);
            _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
            await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
            await _chatService.UpdateUserLastSeenAsync(user.Id);

            var session = await _chatService.CreateSessionAsync(user.Id, deviceInfo);
            _connectionToSession[Context.ConnectionId] = session.Token;

            var history = await _chatService.GetRecentMessagesAsync();
            return new LoginResult
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                Message = "Account created and logged in successfully",
                SessionToken = session.Token,
                MessageHistory = history.Select(m => new MessageDto
                {
                    ConnectionId = "",
                    Username = m.Sender.Username,
                    Content = m.Content,
                    SentAt = m.SentAt
                }).ToList()
            };
        }

        if (!_chatService.VerifyPassword(user, password))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid password"
            };
        }

        _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
        await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
        await _chatService.UpdateUserLastSeenAsync(user.Id);

        var newSession = await _chatService.CreateSessionAsync(user.Id, deviceInfo);
        _connectionToSession[Context.ConnectionId] = newSession.Token;

        var messageHistory = await _chatService.GetRecentMessagesAsync();
        return new LoginResult
        {
            Success = true,
            UserId = user.Id,
            Username = user.Username,
            Message = "Logged in successfully",
            SessionToken = newSession.Token,
            MessageHistory = messageHistory.Select(m => new MessageDto
            {
                ConnectionId = "",
                Username = m.Sender.Username,
                Content = m.Content,
                SentAt = m.SentAt
            }).ToList()
        };
    }

    public async Task<LoginResult> LoginWithSession(string sessionToken)
    {
        var (isValid, user) = await _chatService.ValidateSessionAsync(sessionToken);

        if (!isValid || user == null)
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid or expired session"
            };
        }

        _connectedUsers[Context.ConnectionId] = (user.Id, user.Username);
        _connectionToSession[Context.ConnectionId] = sessionToken;
        await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedIn");
        await _chatService.UpdateUserLastSeenAsync(user.Id);

        var messageHistory = await _chatService.GetRecentMessagesAsync();
        return new LoginResult
        {
            Success = true,
            UserId = user.Id,
            Username = user.Username,
            Message = "Logged in with session successfully",
            SessionToken = sessionToken,
            MessageHistory = messageHistory.Select(m => new MessageDto
            {
                ConnectionId = "",
                Username = m.Sender.Username,
                Content = m.Content,
                SentAt = m.SentAt
            }).ToList()
        };
    }

    public async Task Logout()
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var userInfo))
        {
            if (_connectionToSession.TryRemove(Context.ConnectionId, out var sessionToken))
            {
                Console.WriteLine($"[DEBUG] Revoking session for user {userInfo.Username}: {sessionToken[..16]}...");
                var revoked = await _chatService.RevokeSessionAsync(sessionToken);
                Console.WriteLine($"[DEBUG] Session revoked: {revoked}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] No session token found for {userInfo.Username} (ConnectionId: {Context.ConnectionId})");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "LoggedIn");
            await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
        }
    }

    public async Task<List<SessionInfo>> GetActiveSessions()
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var userInfo))
        {
            return new List<SessionInfo>();
        }

        var sessions = await _chatService.GetUserSessionsAsync(userInfo.UserId);
        return sessions.Select(s => new SessionInfo
        {
            Token = s.Token,
            DeviceInfo = s.DeviceInfo,
            CreatedAt = s.CreatedAt,
            LastActivityAt = s.LastActivityAt,
            ExpiresAt = s.ExpiresAt
        }).ToList();
    }

    public async Task<bool> RevokeSession(string sessionToken)
    {
        if (!_connectedUsers.TryGetValue(Context.ConnectionId, out _))
        {
            return false;
        }

        var success = await _chatService.RevokeSessionAsync(sessionToken);

        if (success)
        {
            // Find if the revoked session belongs to a currently connected user
            var connectionWithSession = _connectionToSession.FirstOrDefault(kvp => kvp.Value == sessionToken);

            if (!connectionWithSession.Equals(default(KeyValuePair<string, string>)))
            {
                var connectionId = connectionWithSession.Key;

                // Notify the user their session was revoked
                await Clients.Client(connectionId).SendAsync("SessionRevoked", "Your session has been revoked");

                // Clean up
                _connectedUsers.TryRemove(connectionId, out var userInfo);
                _connectionToSession.TryRemove(connectionId, out _);
                await Groups.RemoveFromGroupAsync(connectionId, "LoggedIn");

                if (userInfo.UserId > 0)
                {
                    await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
                }
            }
        }

        return success;
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
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var userInfo))
        {
            _connectionToSession.TryRemove(Context.ConnectionId, out _);
            await _chatService.UpdateUserLastSeenAsync(userInfo.UserId);
            Console.WriteLine($"Client disconnected: {Context.ConnectionId} (User: {userInfo.Username})");
        }
        else
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
