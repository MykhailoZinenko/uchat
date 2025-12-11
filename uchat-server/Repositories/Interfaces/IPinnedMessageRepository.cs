using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IPinnedMessageRepository
{
    Task<List<PinnedMessage>> GetByRoomIdAsync(int roomId);
    Task<PinnedMessage?> GetByRoomAndMessageAsync(int roomId, int messageId);
    Task<PinnedMessage> CreateAsync(PinnedMessage pinnedMessage);
    Task DeleteAsync(PinnedMessage pinnedMessage);
}
