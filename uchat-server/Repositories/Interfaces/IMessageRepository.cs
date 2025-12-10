using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id);
    Task<List<Message>> GetByRoomIdAsync(int roomId, int limit, int? beforeMessageId = null);
    Task<Message> CreateAsync(Message message);
}
