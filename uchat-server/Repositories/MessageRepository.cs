using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly UchatDbContext _context;

    public MessageRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(int id)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Message>> GetByRoomIdAsync(int roomId, int limit, int? beforeMessageId = null)
    {
        var query = _context.Messages
            .Where(m => m.RoomId == roomId);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(m => m.Id < beforeMessageId.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        message.SentAt = DateTime.UtcNow;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        return message;
    }

    public async Task<Message> UpdateAsync(Message message)
    {
        _context.Messages.Update(message);
        await _context.SaveChangesAsync();
        return message;
    }
}
