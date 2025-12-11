using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IBlockedUserService
{
    Task<BlockedUser> BlockUserAsync(int blockerId, int blockedUserId);
    Task UnblockUserAsync(int blockerId, int blockedUserId);
    Task<List<BlockedUser>> GetBlockedUsersAsync(int userId);
    Task<List<BlockedUser>> GetBlockersOfUserAsync(int userId);
    Task<bool> IsBlockedAsync(int userId1, int userId2);
}
