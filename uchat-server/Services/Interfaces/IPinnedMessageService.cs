using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IPinnedMessageService
{
    Task<PinnedMessage> PinMessageAsync(int roomId, int messageId, int userId);
    Task UnpinMessageAsync(int roomId, int messageId, int userId);
    Task<List<PinnedMessage>> GetPinnedMessagesAsync(int roomId, int userId);
}
