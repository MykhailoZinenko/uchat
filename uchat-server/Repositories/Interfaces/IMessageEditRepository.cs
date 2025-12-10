using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IMessageEditRepository
{
    Task<MessageEdit?> GetLatestEditAsync(int messageId);
    Task<MessageEdit> CreateAsync(MessageEdit edit);
}
