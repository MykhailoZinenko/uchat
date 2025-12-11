using Microsoft.EntityFrameworkCore;
using uchat_common.Enums;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Repositories.Interfaces;

namespace uchat_server.Repositories;

public class MessageDeliveryStatusRepository : IMessageDeliveryStatusRepository
{
    private readonly UchatDbContext _context;

    public MessageDeliveryStatusRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<MessageDeliveryStatus?> GetAsync(int messageId, int userId)
    {
        return await _context.MessageDeliveryStatuses
            .FirstOrDefaultAsync(d => d.MessageId == messageId && d.UserId == userId);
    }

    public async Task<List<MessageDeliveryStatus>> GetByMessageIdAsync(int messageId)
    {
        return await _context.MessageDeliveryStatuses
            .Where(d => d.MessageId == messageId)
            .Include(d => d.User)
            .ToListAsync();
    }

    public async Task<List<MessageDeliveryStatus>> GetByUserIdAsync(int userId, DeliveryStatus? status = null)
    {
        var query = _context.MessageDeliveryStatuses
            .Where(d => d.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await query
            .Include(d => d.Message)
            .ToListAsync();
    }

    public async Task<List<MessageDeliveryStatus>> GetByMessageIdsAndUserIdAsync(List<int> messageIds, int userId)
    {
        return await _context.MessageDeliveryStatuses
            .Where(d => messageIds.Contains(d.MessageId) && d.UserId == userId)
            .ToListAsync();
    }

    public async Task<MessageDeliveryStatus> CreateAsync(MessageDeliveryStatus deliveryStatus)
    {
        deliveryStatus.CreatedAt = DateTime.UtcNow;
        _context.MessageDeliveryStatuses.Add(deliveryStatus);
        await _context.SaveChangesAsync();
        return deliveryStatus;
    }

    public async Task UpdateStatusAsync(int messageId, int userId, DeliveryStatus status)
    {
        var deliveryStatus = await GetAsync(messageId, userId);
        if (deliveryStatus == null)
        {
            return;
        }

        deliveryStatus.Status = status;

        if (status == DeliveryStatus.Delivered && deliveryStatus.DeliveredAt == null)
        {
            deliveryStatus.DeliveredAt = DateTime.UtcNow;
        }
        else if (status == DeliveryStatus.Read && deliveryStatus.ReadAt == null)
        {
            deliveryStatus.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId, int roomId)
    {
        return await _context.MessageDeliveryStatuses
            .Include(d => d.Message)
            .Where(d => d.UserId == userId
                     && d.Message.RoomId == roomId
                     && d.Status != DeliveryStatus.Read)
            .CountAsync();
    }
}
