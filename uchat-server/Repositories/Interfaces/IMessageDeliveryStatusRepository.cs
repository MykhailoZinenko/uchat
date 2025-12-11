using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories.Interfaces;

public interface IMessageDeliveryStatusRepository
{
    Task<MessageDeliveryStatus?> GetAsync(int messageId, int userId);
    Task<List<MessageDeliveryStatus>> GetByMessageIdAsync(int messageId);
    Task<List<MessageDeliveryStatus>> GetByUserIdAsync(int userId, DeliveryStatus? status = null);
    Task<List<MessageDeliveryStatus>> GetByMessageIdsAndUserIdAsync(List<int> messageIds, int userId);
    Task<MessageDeliveryStatus> CreateAsync(MessageDeliveryStatus deliveryStatus);
    Task UpdateStatusAsync(int messageId, int userId, DeliveryStatus status);
    Task<int> GetUnreadCountAsync(int userId, int roomId);
}
