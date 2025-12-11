using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public class ChatService
{
    private readonly UchatDbContext _db;

    public ChatService(UchatDbContext db)
    {
        _db = db;
    }

    public async Task<List<Message>> GetRecentMessagesAsync(int roomId, int limit = 50)
    {
        return await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message> SaveMessageAsync(int roomId, int senderId, string content)
    {
        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();

        return message;
    }

    public async Task UpdateUserLastSeenAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}

