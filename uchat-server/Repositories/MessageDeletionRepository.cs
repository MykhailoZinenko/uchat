using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class MessageDeletionRepository : IMessageDeletionRepository
{
    private readonly UchatDbContext _context;

    public MessageDeletionRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<MessageDeletion?> GetByMessageIdAsync(int messageId)
    {
        return await _context.MessageDeletions
            .FirstOrDefaultAsync(d => d.MessageId == messageId);
    }

    public async Task<MessageDeletion> CreateAsync(MessageDeletion deletion)
    {
        deletion.DeletedAt = DateTime.UtcNow;
        _context.MessageDeletions.Add(deletion);
        await _context.SaveChangesAsync();
        return deletion;
    }

    public async Task<List<int>> GetDeletedMessageIdsAsync(List<int> messageIds)
    {
        return await _context.MessageDeletions
            .Where(d => messageIds.Contains(d.MessageId))
            .Select(d => d.MessageId)
            .ToListAsync();
    }
}
