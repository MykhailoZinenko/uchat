using Microsoft.AspNetCore.SignalR;
using uchat_server.Services;
using uchat_common.Dtos;
using uchat_common.Enums;
using uchat_server.Exceptions;

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

    public ChatHub(
        IAuthService authService, 
        ISessionService sessionService, 
        ICryptographyService cryptographyService,
        IUserService userService,
        IErrorMapper errorMapper,
        IRoomService roomService,
        IRoomMemberService roomMemberService,
        IMessageService messageService,
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
    }

    // ==================== AUTH ENDPOINTS ====================

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

    public async Task<ApiResponse<List<RoomDto>>> GetAccessibleRooms(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var rooms = await _roomService.GetAccessibleRoomsAsync(session.UserId);

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

    public async Task<ApiResponse<MessageDto>> SendMessage(string sessionToken, int roomId, string content, int? replyToMessageId = null)
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
                SentAt = message.SentAt
            };

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
            var messages = await _messageService.GetMessagesAsync(roomId, session.UserId, limit, beforeMessageId);

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
            await _messageService.EditMessageAsync(messageId, session.UserId, newContent);

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
            await _messageService.DeleteMessageAsync(messageId, session.UserId);

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

    // ==================== PINNED MESSAGES ENDPOINTS ====================

    public async Task<ApiResponse<bool>> PinMessage(string sessionToken, int roomId, int messageId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _pinnedMessageService.PinMessageAsync(roomId, messageId, session.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Message pinned",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> UnpinMessage(string sessionToken, int roomId, int messageId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _pinnedMessageService.UnpinMessageAsync(roomId, messageId, session.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Message unpinned",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<List<MessageDto>>> GetPinnedMessages(string sessionToken, int roomId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var pinnedMessages = await _pinnedMessageService.GetPinnedMessagesAsync(roomId, session.UserId);

            var messageDtos = pinnedMessages.Select(p => new MessageDto
            {
                Id = p.Message.Id,
                RoomId = p.Message.RoomId,
                SenderUserId = p.Message.SenderUserId,
                SenderUsername = p.Message.Sender?.Username,
                MessageType = p.Message.MessageType,
                ServiceAction = p.Message.ServiceAction,
                ReplyToMessageId = p.Message.ReplyToMessageId,
                Content = p.Message.Content,
                SentAt = p.Message.SentAt
            }).ToList();

            return new ApiResponse<List<MessageDto>>
            {
                Success = true,
                Data = messageDtos
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<MessageDto>>(ex);
        }
    }

    // ==================== FRIENDSHIP ENDPOINTS ====================

    public async Task<ApiResponse<FriendshipDto>> SendFriendRequest(string sessionToken, int toUserId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var friendship = await _friendshipService.SendFriendRequestAsync(session.UserId, toUserId);

            return new ApiResponse<FriendshipDto>
            {
                Success = true,
                Message = "Friend request sent",
                Data = MapFriendship(friendship, session.UserId)
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<FriendshipDto>(ex);
        }
    }

    public async Task<ApiResponse<FriendshipDto>> AcceptFriendRequest(string sessionToken, int friendshipId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var friendship = await _friendshipService.AcceptFriendRequestAsync(friendshipId, session.UserId);

            return new ApiResponse<FriendshipDto>
            {
                Success = true,
                Message = "Friend request accepted",
                Data = MapFriendship(friendship, session.UserId)
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<FriendshipDto>(ex);
        }
    }

    public async Task<ApiResponse<FriendshipDto>> RejectFriendRequest(string sessionToken, int friendshipId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var friendship = await _friendshipService.RejectFriendRequestAsync(friendshipId, session.UserId);

            return new ApiResponse<FriendshipDto>
            {
                Success = true,
                Message = "Friend request rejected",
                Data = MapFriendship(friendship, session.UserId)
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<FriendshipDto>(ex);
        }
    }

    public async Task<ApiResponse<bool>> RemoveFriend(string sessionToken, int friendshipId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _friendshipService.RemoveFriendAsync(friendshipId, session.UserId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Friend removed",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<List<FriendshipDto>>> GetFriends(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var friendships = await _friendshipService.GetFriendsAsync(session.UserId);

            return new ApiResponse<List<FriendshipDto>>
            {
                Success = true,
                Data = friendships.Select(f => MapFriendship(f, session.UserId)).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<FriendshipDto>>(ex);
        }
    }

    public async Task<ApiResponse<List<FriendshipDto>>> GetPendingFriendRequests(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var friendships = await _friendshipService.GetPendingRequestsAsync(session.UserId);

            return new ApiResponse<List<FriendshipDto>>
            {
                Success = true,
                Data = friendships.Select(f => MapFriendship(f, session.UserId)).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<FriendshipDto>>(ex);
        }
    }

    private FriendshipDto MapFriendship(uchat_server.Data.Entities.Friendship f, int currentUserId)
    {
        var friendId = f.User1Id == currentUserId ? f.User2Id : f.User1Id;
        var friend = f.User1Id == currentUserId ? f.User2 : f.User1;

        return new FriendshipDto
        {
            Id = f.Id,
            FriendUserId = friendId,
            FriendUsername = friend?.Username ?? "Unknown",
            Status = f.Status,
            IsInitiator = f.InitiatedByUserId == currentUserId,
            CreatedAt = f.CreatedAt,
            RespondedAt = f.RespondedAt
        };
    }

    // ==================== BLOCKED USERS ENDPOINTS ====================

    public async Task<ApiResponse<bool>> BlockUser(string sessionToken, int userId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _blockedUserService.BlockUserAsync(session.UserId, userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "User blocked",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<bool>> UnblockUser(string sessionToken, int userId)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            await _blockedUserService.UnblockUserAsync(session.UserId, userId);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "User unblocked",
                Data = true
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<bool>(ex);
        }
    }

    public async Task<ApiResponse<List<UserDto>>> GetBlockedUsers(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var blocked = await _blockedUserService.GetBlockedUsersAsync(session.UserId);

            return new ApiResponse<List<UserDto>>
            {
                Success = true,
                Data = blocked.Select(b => new UserDto
                {
                    Id = b.Blocked.Id,
                    Username = b.Blocked.Username,
                    DisplayName = b.Blocked.DisplayName,
                    AvatarUrl = b.Blocked.AvatarUrl,
                    IsOnline = b.Blocked.IsOnline
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<UserDto>>(ex);
        }
    }

    public async Task<ApiResponse<List<UserDto>>> GetBlockersOfMe(string sessionToken)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var blockers = await _blockedUserService.GetBlockersOfUserAsync(session.UserId);

            return new ApiResponse<List<UserDto>>
            {
                Success = true,
                Data = blockers.Select(b => new UserDto
                {
                    Id = b.Blocker.Id,
                    Username = b.Blocker.Username,
                    DisplayName = b.Blocker.DisplayName,
                    AvatarUrl = b.Blocker.AvatarUrl,
                    IsOnline = b.Blocker.IsOnline
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<UserDto>>(ex);
        }
    }

    // ==================== SEARCH ENDPOINTS ====================

    public async Task<ApiResponse<List<UserDto>>> SearchUsers(string sessionToken, string query, int limit = 20)
    {
        try
        {
            await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var users = await _searchService.SearchUsersAsync(query, limit);

            return new ApiResponse<List<UserDto>>
            {
                Success = true,
                Data = users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    IsOnline = u.IsOnline
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<UserDto>>(ex);
        }
    }

    public async Task<ApiResponse<List<MessageDto>>> SearchMessages(string sessionToken, string query, int? roomId = null, int limit = 50)
    {
        try
        {
            var session = await _cryptographyService.ValidateSessionTokenAsync(sessionToken);
            var messages = await _searchService.SearchMessagesAsync(query, session.UserId, roomId, limit);

            return new ApiResponse<List<MessageDto>>
            {
                Success = true,
                Data = messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    RoomId = m.RoomId,
                    SenderUserId = m.SenderUserId,
                    SenderUsername = m.Sender?.Username,
                    MessageType = m.MessageType,
                    ServiceAction = m.ServiceAction,
                    ReplyToMessageId = m.ReplyToMessageId,
                    Content = m.Content,
                    SentAt = m.SentAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return _errorMapper.MapException<List<MessageDto>>(ex);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}


