using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IBlockedUserRepository
{
    Task<BlockedUser?> GetAsync(int blockerUserId, int blockedUserId);
    Task<List<BlockedUser>> GetBlockedByUserAsync(int userId);
    Task<List<BlockedUser>> GetBlockersOfUserAsync(int userId);
    Task<bool> IsBlockedAsync(int userId1, int userId2);
    Task<BlockedUser> CreateAsync(BlockedUser blockedUser);
    Task DeleteAsync(BlockedUser blockedUser);
}
