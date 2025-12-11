using uchat_server.Data.Entities;

namespace uchat_server.Repositories.Interfaces;

public interface IUserPtsRepository
{
    Task<UserPts?> GetByUserIdAsync(int userId);
    Task<int> GetCurrentPtsAsync(int userId);
    Task<int> IncrementPtsAsync(int userId);
    Task InitializeAsync(int userId);
}
