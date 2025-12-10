using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IMessageDeletionRepository
{
    Task<MessageDeletion?> GetByMessageIdAsync(int messageId);
    Task<MessageDeletion> CreateAsync(MessageDeletion deletion);
    Task<List<int>> GetDeletedMessageIdsAsync(List<int> messageIds);
}
