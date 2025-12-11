using uchat_common.Dtos;
using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IMessageService
{
    Task<Message> SendMessageAsync(int roomId, int senderUserId, string content, int? replyToMessageId = null);
    Task<Message> SendSystemMessageAsync(int roomId, ServiceAction action, string content, int? relatedUserId = null);
    Task<List<MessageDto>> GetMessagesAsync(int roomId, int userId, int limit = 50, int? beforeMessageId = null);
    Task<Message> EditMessageAsync(int messageId, int userId, string newContent);
    Task DeleteMessageAsync(int messageId, int userId);
    Task<int> GetNextPtsAsync(int userId);
    Task<int> IncrementPtsAsync(int userId);
    Task<int> GetUnreadCountAsync(int roomId, int userId);
    Task AddUserUpdateAsync(int userId, UserUpdateDto update);
    Task<List<UserUpdateDto>> GetUserUpdatesAsync(int userId, int fromPts, int limit = 100);

    // Telegram-like delivery tracking
    Task MarkMessageAsDeliveredAsync(int messageId, int userId);
    Task MarkMessageAsReadAsync(int messageId, int userId);
    Task<List<MessageQueue>> GetPendingMessagesAsync(int userId);
    Task QueueMessageForOfflineUserAsync(int messageId, int recipientUserId);
}

