using uchat_server.Data.Entities;

namespace uchat_server.Repositories.Interfaces;

public interface IMessageQueueRepository
{
    Task<List<MessageQueue>> GetPendingMessagesAsync(int userId);
    Task QueueMessageAsync(int messageId, int recipientUserId);
    Task MarkAsDeliveredAsync(int queueId);
    Task MarkAllAsDeliveredAsync(int userId);
}
