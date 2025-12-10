using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class PinnedMessageRepository : IPinnedMessageRepository
{
    private readonly UchatDbContext _context;

    public PinnedMessageRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<List<PinnedMessage>> GetByRoomIdAsync(int roomId)
    {
        return await _context.PinnedMessages
            .Where(p => p.RoomId == roomId)
            .Include(p => p.Message)
            .ThenInclude(m => m.Sender)
            .OrderByDescending(p => p.PinnedAt)
            .ToListAsync();
    }

    public async Task<PinnedMessage?> GetByRoomAndMessageAsync(int roomId, int messageId)
    {
        return await _context.PinnedMessages
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.MessageId == messageId);
    }

    public async Task<PinnedMessage> CreateAsync(PinnedMessage pinnedMessage)
    {
        pinnedMessage.PinnedAt = DateTime.UtcNow;
        _context.PinnedMessages.Add(pinnedMessage);
        await _context.SaveChangesAsync();
        return pinnedMessage;
    }

    public async Task DeleteAsync(PinnedMessage pinnedMessage)
    {
        _context.PinnedMessages.Remove(pinnedMessage);
        await _context.SaveChangesAsync();
    }
}
