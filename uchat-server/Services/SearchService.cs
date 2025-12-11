using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class SearchService : ISearchService
{
    private readonly UchatDbContext _context;
    private readonly IRoomMemberRepository _roomMemberRepository;
    private readonly IMessageDeletionRepository _messageDeletionRepository;

    public SearchService(
        UchatDbContext context, 
        IRoomMemberRepository roomMemberRepository,
        IMessageDeletionRepository messageDeletionRepository)
    {
        _context = context;
        _roomMemberRepository = roomMemberRepository;
        _messageDeletionRepository = messageDeletionRepository;
    }

    public async Task<List<User>> SearchUsersAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<User>();
        }

        var lowerQuery = query.ToLower();

        return await _context.Users
            .Where(u => u.Username.ToLower().Contains(lowerQuery) ||
                        (u.DisplayName != null && u.DisplayName.ToLower().Contains(lowerQuery)))
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Message>> SearchMessagesAsync(string query, int userId, int? roomId = null, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new List<Message>();
        }

        var lowerQuery = query.ToLower();
        
        var accessibleRoomIds = await _roomMemberRepository.GetAccessibleRoomIdsAsync(userId);

        var messagesQuery = _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Room)
            .Where(m => m.Content.ToLower().Contains(lowerQuery));

        if (roomId.HasValue)
        {
            if (!accessibleRoomIds.Contains(roomId.Value))
            {
                return new List<Message>();
            }
            messagesQuery = messagesQuery.Where(m => m.RoomId == roomId.Value);
        }
        else
        {
            messagesQuery = messagesQuery.Where(m => accessibleRoomIds.Contains(m.RoomId));
        }

        var messages = await messagesQuery
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        var messageIds = messages.Select(m => m.Id).ToList();
        var deletedIds = await _messageDeletionRepository.GetDeletedMessageIdsAsync(messageIds);

        return messages.Where(m => !deletedIds.Contains(m.Id)).ToList();
    }
}
