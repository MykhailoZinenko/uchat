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
}

