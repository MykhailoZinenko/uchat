using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Repositories.Interfaces;

namespace uchat_server.Repositories;

public class MessageQueueRepository : IMessageQueueRepository
{
    private readonly UchatDbContext _context;

    public MessageQueueRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<List<MessageQueue>> GetPendingMessagesAsync(int userId)
    {
        return await _context.MessageQueues
            .Where(q => q.RecipientUserId == userId && !q.IsDelivered)
            .Include(q => q.Message)
                .ThenInclude(m => m.Sender)
            .OrderBy(q => q.QueuedAt)
            .ToListAsync();
    }

    public async Task QueueMessageAsync(int messageId, int recipientUserId)
    {
        var queueItem = new MessageQueue
        {
            MessageId = messageId,
            RecipientUserId = recipientUserId,
            QueuedAt = DateTime.UtcNow,
            IsDelivered = false
        };

        _context.MessageQueues.Add(queueItem);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsDeliveredAsync(int queueId)
    {
        var queueItem = await _context.MessageQueues.FindAsync(queueId);
        if (queueItem != null)
        {
            queueItem.IsDelivered = true;
            queueItem.DeliveredAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsDeliveredAsync(int userId)
    {
        var pendingMessages = await _context.MessageQueues
            .Where(q => q.RecipientUserId == userId && !q.IsDelivered)
            .ToListAsync();

        foreach (var item in pendingMessages)
        {
            item.IsDelivered = true;
            item.DeliveredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
