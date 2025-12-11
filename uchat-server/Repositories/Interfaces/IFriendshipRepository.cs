using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(int id);
    Task<Friendship?> GetByUsersAsync(int userId1, int userId2);
    Task<List<Friendship>> GetByUserAsync(int userId, FriendshipStatus? status = null);
    Task<Friendship> CreateAsync(Friendship friendship);
    Task UpdateAsync(Friendship friendship);
    Task DeleteAsync(Friendship friendship);
}
