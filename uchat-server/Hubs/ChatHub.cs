using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using uchat_server.Services;
using uchat_common.Dtos;
using uchat_common.Enums;
using uchat_server.Exceptions;
using System.Threading.Tasks;
using uchat_server.Repositories;

namespace uchat_server.Hubs;

public class ChatHub : Hub
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;
    private readonly ICryptographyService _cryptographyService;
    private readonly IUserService _userService;
    private readonly IErrorMapper _errorMapper;
    private readonly IRoomService _roomService;
    private readonly IRoomMemberService _roomMemberService;
    private readonly IMessageService _messageService;
    private readonly IPinnedMessageService _pinnedMessageService;
    private readonly IFriendshipService _friendshipService;
    private readonly IBlockedUserService _blockedUserService;
    private readonly ISearchService _searchService;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ChatHub> _logger;

    private static string GetRoomGroupName(int roomId) => $"room:{roomId}";
    private static string GetUserGroupName(int userId) => $"user:{userId}";
    public ChatHub(
        IAuthService authService,
        ISessionService sessionService,
        ICryptographyService cryptographyService,
        IUserService userService,
        IErrorMapper errorMapper,
        IRoomService roomService,
        IRoomMemberService roomMemberService,
        IMessageService messageService,
        IMessageRepository messageRepository,
        ILogger<ChatHub> logger,
        IPinnedMessageService pinnedMessageService,
        IFriendshipService friendshipService,
        IBlockedUserService blockedUserService,
        ISearchService searchService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _cryptographyService = cryptographyService;
        _userService = userService;
        _errorMapper = errorMapper;
        _roomService = roomService;
        _roomMemberService = roomMemberService;
        _messageService = messageService;
        _pinnedMessageService = pinnedMessageService;
        _friendshipService = friendshipService;
        _blockedUserService = blockedUserService;
        _searchService = searchService;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    // ==================== AUTH ENDPOINTS ====================

    public async Task<ApiResponse<AuthDto>> Register(string username, string password, string? deviceInfo = null, string? ipAddress = null)
    {
        try
        {
            string finalDeviceInfo = deviceInfo ?? "Unknown";
            string finalIpAddress = ipAddress ?? Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            _logger.LogInformation("Register attempt Username={Username} Device={Device} Ip={Ip}", username, finalDeviceInfo, finalIpAddress);

            AuthDto authDto = await _authService.RegisterAsync(username, password, finalDeviceInfo, finalIpAddress);

            _logger.LogInformation("Register succeeded Username={Username}", username);

            return new ApiResponse<AuthDto>
            {
                Success = true,
                Message = "Account created successfully",
                Data = authDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register failed for Username={Username} Device={DeviceInfo} Ip={IpAddress}", username, deviceInfo, ipAddress);
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
            _logger.LogInformation("Logout requested. UserId={UserId} SessionId={SessionId} Success={Success}", session.UserId, session.Id, success);

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

    public async Task<ApiResponse<bool>> RegisterSession(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(session.Id));
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(session.UserId));
            _logger.LogInformation("RegisterSession: ConnectionId={ConnectionId} SessionId={SessionId}", Context.ConnectionId, session.Id);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Session registered",
                Data = true
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
            _logger.LogDebug("GetActiveSessions for UserId={UserId} Count={Count}", session.UserId, sessions.Count);
            var sessionInfos = sessions.Select(s => new SessionInfo
            {
                Id = s.Id,
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
            var targetSession = await _sessionService.GetSessionByIdAsync(sessionId);
            if (targetSession.UserId != session.UserId)
            {
                throw new AppException("Session does not belong to the current user");
            }

            if (targetSession.Id == session.Id)
            {
                throw new AppException("Cannot revoke the current session");
            }

            var success = await _sessionService.RevokeSessionAsync(sessionId);
            _logger.LogInformation("RevokeSession requested by UserId={UserId} TargetSessionId={SessionId} Success={Success}", session.UserId, sessionId, success);
            if (success)
            {
                await Clients.Group(GetSessionGroupName(targetSession.Id)).SendAsync("SessionRevoked");
            }

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

    public async Task<ApiResponse<bool>> RevokeAllSessions(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var activeSessions = await _sessionService.GetActiveSessionsByUserIdAsync(session.UserId);
            var revokedSessions = activeSessions.Where(s => s.Id != session.Id).ToList();

            await _sessionService.RevokeAllUserSessionsExceptAsync(session.UserId, session.Id);
            _logger.LogInformation("RevokeAllSessions requested by UserId={UserId} KeepingSessionId={SessionId}", session.UserId, session.Id);

            foreach (var revoked in revokedSessions)
            {
                await Clients.Group(GetSessionGroupName(revoked.Id)).SendAsync("SessionRevoked");
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Other sessions revoked",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> RevokeSessions(string sessionToken, List<int> sessionIds)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var activeSessions = await _sessionService.GetActiveSessionsByUserIdAsync(session.UserId);

            var validSessionIds = activeSessions
                .Where(s => sessionIds.Contains(s.Id) && s.Id != session.Id)
                .Select(s => s.Id)
                .ToList();

            if (validSessionIds.Count == 0)
            {
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "No sessions to revoke",
                    Data = true
                };
            }

            var success = await _sessionService.RevokeSessionsAsync(validSessionIds);
            _logger.LogInformation("RevokeSessions requested by UserId={UserId} Count={Count}", session.UserId, validSessionIds.Count);
            foreach (var revokedId in validSessionIds)
            {
                await Clients.Group(GetSessionGroupName(revokedId)).SendAsync("SessionRevoked");
            }

            return new ApiResponse<bool>
            {
                Success = success,
                Message = success ? "Selected sessions revoked" : "Failed to revoke selected sessions",
                Data = success
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<List<RoomDto>>> GetAccessibleRooms(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var rooms = await _roomService.GetAccessibleRoomsAsync(session.UserId);
            _logger.LogInformation("GetAccessibleRooms for UserId={UserId} Count={Count}", session.UserId, rooms.Count);

            var roomDtos = rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                RoomType = r.RoomType,
                RoomName = r.RoomName,
                RoomDescription = r.RoomDescription,
                AvatarUrl = r.AvatarUrl,
                IsGlobal = r.IsGlobal,
                CreatedByUserId = r.CreatedByUserId,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new ApiResponse<List<RoomDto>>
            {
                Success = true,
                Data = roomDtos
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<RoomDto>>(ex);
        }
    }

    public async Task<ApiResponse<RoomDto>> CreateRoom(string sessionToken, string type, string? name, string? description)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);

            if (!Enum.TryParse<RoomType>(type, true, out var roomType))
            {
                return new ApiResponse<RoomDto>
                {
                    Success = false,
                    Message = "Invalid room type"
                };
            }

            var room = await _roomService.CreateRoomAsync(session.UserId, roomType, name, description);
            _logger.LogInformation("Room created: RoomId={RoomId} Type={RoomType} CreatorUserId={UserId}", room.Id, room.RoomType, session.UserId);

            return new ApiResponse<RoomDto>
            {
                Success = true,
                Message = "Room created successfully",
                Data = new RoomDto
                {
                    Id = room.Id,
                    RoomType = room.RoomType,
                    RoomName = room.RoomName,
                    RoomDescription = room.RoomDescription,
                    AvatarUrl = room.AvatarUrl,
                    IsGlobal = room.IsGlobal,
                    CreatedByUserId = room.CreatedByUserId,
                    CreatedAt = room.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<RoomDto>(ex);
        }
    }

    public async Task<ApiResponse<RoomDto>> UpdateRoom(string sessionToken, int roomId, string? name, string? description, string? avatarUrl)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var room = await _roomService.UpdateRoomAsync(roomId, session.UserId, name, description, avatarUrl);
            _logger.LogInformation("Room updated: RoomId={RoomId} ByUserId={UserId}", room.Id, session.UserId);

            return new ApiResponse<RoomDto>
            {
                Success = true,
                Message = "Room updated successfully",
                Data = new RoomDto
                {
                    Id = room.Id,
                    RoomType = room.RoomType,
                    RoomName = room.RoomName,
                    RoomDescription = room.RoomDescription,
                    AvatarUrl = room.AvatarUrl,
                    IsGlobal = room.IsGlobal,
                    CreatedByUserId = room.CreatedByUserId,
                    CreatedAt = room.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<RoomDto>(ex);
        }
    }

    public async Task<ApiResponse<bool>> DeleteRoom(string sessionToken, int roomId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomService.DeleteRoomAsync(roomId, session.UserId);
            _logger.LogWarning("Room deleted: RoomId={RoomId} ByUserId={UserId}", roomId, session.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Room deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> AddRoomMembers(string sessionToken, int roomId, List<int> userIds)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomMemberService.AddMembersAsync(roomId, session.UserId, userIds);
            _logger.LogInformation("Members added: RoomId={RoomId} ByUserId={UserId} Count={Count}", roomId, session.UserId, userIds.Count);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Members added successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> AddRoomMember(string sessionToken, int roomId, int userId)
    {
        return await AddRoomMembers(sessionToken, roomId, new List<int> { userId });
    }

    public async Task<ApiResponse<bool>> RemoveRoomMembers(string sessionToken, int roomId, List<int> userIds)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomMemberService.RemoveMembersAsync(roomId, session.UserId, userIds);
            _logger.LogInformation("Members removed: RoomId={RoomId} ByUserId={UserId} Count={Count}", roomId, session.UserId, userIds.Count);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Members removed successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> UpdateMemberRole(string sessionToken, int roomId, int userId, string role)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);

            if (!Enum.TryParse<MemberRole>(role, true, out var memberRole))
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Invalid member role"
                };
            }

            await _roomMemberService.UpdateMemberRoleAsync(roomId, session.UserId, userId, memberRole);
            _logger.LogInformation("Member role updated: RoomId={RoomId} TargetUserId={TargetUserId} NewRole={Role} ByUserId={UserId}", roomId, userId, memberRole, session.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Member role updated successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> UpdateMemberMuted(string sessionToken, int roomId, bool isMuted)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomMemberService.UpdateMutedStatusAsync(roomId, session.UserId, isMuted);
            _logger.LogInformation("Member mute updated: RoomId={RoomId} UserId={UserId} Muted={Muted}", roomId, session.UserId, isMuted);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = isMuted ? "Room muted" : "Room unmuted",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<MessageDto>> SendMessage(string sessionToken, int roomId, string content, int? replyToMessageId = null, string? clientMessageId = null)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var user = await _userService.GetUserByIdAsync(session.UserId);

            if (user == null)
            {
                return new ApiResponse<MessageDto>
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var message = await _messageService.SendMessageAsync(roomId, session.UserId, content, replyToMessageId);
            _logger.LogDebug("Message sent: MessageId={MessageId} RoomId={RoomId} UserId={UserId}", message.Id, roomId, session.UserId);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                RoomId = message.RoomId,
                SenderUserId = message.SenderUserId,
                SenderUsername = user.Username,
                MessageType = message.MessageType,
                ServiceAction = message.ServiceAction,
                ReplyToMessageId = message.ReplyToMessageId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsEdited = false,
                IsDeleted = false
            };

            if (clientMessageId != null)
            {
                var ack = new MessageAckDto
                {
                    ClientMessageId = clientMessageId,
                    ServerMessageId = message.Id,
                    SentAt = message.SentAt
                };
                await Clients.Group(GetUserGroupName(session.UserId)).SendAsync("MessageAck", ack);
            }

            var room = await _roomService.GetRoomByIdAsync(roomId);
            var recipients = new HashSet<int>();

            if (room != null)
            {
                var members = await _roomMemberService.GetMemberUserIdsAsync(roomId);
                foreach (var m in members)
                {
                    recipients.Add(m);
                }
            }

            foreach (var userId in recipients.Where(id => id != session.UserId))
            {
                await Clients.Group(GetUserGroupName(userId)).SendAsync("MessageReceived", messageDto);
            }

            await Clients.GroupExcept(GetUserGroupName(session.UserId), Context.ConnectionId).SendAsync("MessageReceived", messageDto);

            return new ApiResponse<MessageDto>
            {
                Success = true,
                Data = messageDto
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<MessageDto>(ex);
        }
    }

    public async Task<ApiResponse<List<MessageDto>>> GetMessages(string sessionToken, int roomId, int limit = 50, int? beforeMessageId = null)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var messages = await _messageService.GetMessagesAsync(roomId, limit, beforeMessageId);
            _logger.LogDebug("Messages fetched: RoomId={RoomId} UserId={UserId} Count={Count}", roomId, session.UserId, messages.Count);

            return new ApiResponse<List<MessageDto>>
            {
                Success = true,
                Data = messages
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<MessageDto>>(ex);
        }
    }

    public async Task<ApiResponse<bool>> EditMessage(string sessionToken, int messageId, string newContent)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var message = await _messageService.EditMessageAsync(messageId, session.UserId, newContent);
            _logger.LogInformation("Message edited: MessageId={MessageId} UserId={UserId}", messageId, session.UserId);

            var room = await _roomService.GetRoomByIdAsync(message.RoomId);
            if (room != null)
            {
                var members = await _roomMemberService.GetMemberUserIdsAsync(message.RoomId);
                foreach (var userId in members)
                {
                    await Clients.Group(GetUserGroupName(userId)).SendAsync("MessageEdited", messageId, newContent);
                }
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Message edited",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> DeleteMessage(string sessionToken, int messageId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Message not found"
                };
            }

            await _messageService.DeleteMessageAsync(messageId, session.UserId);
            _logger.LogInformation("Message deleted: MessageId={MessageId} UserId={UserId}", messageId, session.UserId);

            var room = await _roomService.GetRoomByIdAsync(message.RoomId);
            if (room != null)
            {
                var members = await _roomMemberService.GetMemberUserIdsAsync(message.RoomId);
                foreach (var userId in members)
                {
                    await Clients.Group(GetUserGroupName(userId)).SendAsync("MessageDeleted", messageId);
                }
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Message deleted",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> JoinRoom(string sessionToken, int roomId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomMemberService.JoinRoomAsync(roomId, session.UserId);
            _logger.LogInformation("User joined room: RoomId={RoomId} UserId={UserId}", roomId, session.UserId);

            await Groups.AddToGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
            await Clients.Group(GetUserGroupName(session.UserId)).SendAsync("RoomJoined", roomId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Joined room",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> LeaveRoom(string sessionToken, int roomId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _roomMemberService.LeaveRoomAsync(roomId, session.UserId);
            _logger.LogInformation("User left room: RoomId={RoomId} UserId={UserId}", roomId, session.UserId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoomGroupName(roomId));
            await Clients.Group(GetUserGroupName(session.UserId)).SendAsync("RoomLeft", roomId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Left room",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }


    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: ConnectionId={ConnectionId} RemoteIp={RemoteIp}",
            Context.ConnectionId,
            Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        await base.OnConnectedAsync();
    }

    private static string GetSessionGroupName(int sessionId) => $"session:{sessionId}";

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: ConnectionId={ConnectionId} Error={Error}",
            Context.ConnectionId,
            exception?.Message ?? "none");
        await base.OnDisconnectedAsync(exception);
    }
}
