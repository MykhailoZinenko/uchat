using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class MessageEditRepository : IMessageEditRepository
{
    private readonly UchatDbContext _context;

    public MessageEditRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<MessageEdit?> GetLatestEditAsync(int messageId)
    {
        return await _context.MessageEdits
            .Where(e => e.MessageId == messageId)
            .OrderByDescending(e => e.EditedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<MessageEdit> CreateAsync(MessageEdit edit)
    {
        edit.EditedAt = DateTime.UtcNow;
        _context.MessageEdits.Add(edit);
        await _context.SaveChangesAsync();
        return edit;
    }
}
