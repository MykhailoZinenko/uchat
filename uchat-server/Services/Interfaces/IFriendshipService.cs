using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IFriendshipService
{
    Task<Friendship> SendFriendRequestAsync(int fromUserId, int toUserId);
    Task<Friendship> AcceptFriendRequestAsync(int friendshipId, int userId);
    Task<Friendship> RejectFriendRequestAsync(int friendshipId, int userId);
    Task RemoveFriendAsync(int friendshipId, int userId);
    Task<List<Friendship>> GetFriendsAsync(int userId);
    Task<List<Friendship>> GetPendingRequestsAsync(int userId);
}
