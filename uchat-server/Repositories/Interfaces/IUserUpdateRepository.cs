using uchat_server.Data.Entities;

namespace uchat_server.Repositories.Interfaces;

public interface IUserUpdateRepository
{
    Task<List<UserUpdate>> GetUpdatesAsync(int userId, int fromPts, int limit = 100);
    Task<UserUpdate> CreateUpdateAsync(int userId, int pts, string updateType, string updateData);
    Task CleanupOldUpdatesAsync(DateTime before);
}
