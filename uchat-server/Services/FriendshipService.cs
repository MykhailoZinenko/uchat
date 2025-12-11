using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class FriendshipService : IFriendshipService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;

    public FriendshipService(IFriendshipRepository friendshipRepository, IUserRepository userRepository)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
    }

    public async Task<Friendship> SendFriendRequestAsync(int fromUserId, int toUserId)
    {
        if (fromUserId == toUserId)
        {
            throw new ValidationException("Cannot send friend request to yourself");
        }

        var toUser = await _userRepository.GetByIdAsync(toUserId);
        if (toUser == null)
        {
            throw new NotFoundException("User not found");
        }

        var existing = await _friendshipRepository.GetByUsersAsync(fromUserId, toUserId);
        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
            {
                throw new ValidationException("Already friends");
            }
            if (existing.Status == FriendshipStatus.Pending)
            {
                throw new ValidationException("Friend request already pending");
            }
            if (existing.Status == FriendshipStatus.Rejected)
            {
                existing.Status = FriendshipStatus.Pending;
                existing.InitiatedByUserId = fromUserId;
                existing.CreatedAt = DateTime.UtcNow;
                existing.RespondedAt = null;
                await _friendshipRepository.UpdateAsync(existing);
                return existing;
            }
        }

        var friendship = new Friendship
        {
            User1Id = Math.Min(fromUserId, toUserId),
            User2Id = Math.Max(fromUserId, toUserId),
            InitiatedByUserId = fromUserId,
            Status = FriendshipStatus.Pending
        };

        return await _friendshipRepository.CreateAsync(friendship);
    }

    public async Task<Friendship> AcceptFriendRequestAsync(int friendshipId, int userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null)
        {
            throw new NotFoundException("Friendship request not found");
        }

        if (friendship.InitiatedByUserId == userId)
        {
            throw new ForbiddenException("Cannot accept your own friend request");
        }

        if (friendship.User1Id != userId && friendship.User2Id != userId)
        {
            throw new ForbiddenException("You are not part of this friend request");
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            throw new ValidationException("Friend request is not pending");
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.RespondedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship);

        return friendship;
    }

    public async Task<Friendship> RejectFriendRequestAsync(int friendshipId, int userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null)
        {
            throw new NotFoundException("Friendship request not found");
        }

        if (friendship.InitiatedByUserId == userId)
        {
            throw new ForbiddenException("Cannot reject your own friend request");
        }

        if (friendship.User1Id != userId && friendship.User2Id != userId)
        {
            throw new ForbiddenException("You are not part of this friend request");
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            throw new ValidationException("Friend request is not pending");
        }

        friendship.Status = FriendshipStatus.Rejected;
        friendship.RespondedAt = DateTime.UtcNow;
        await _friendshipRepository.UpdateAsync(friendship);

        return friendship;
    }

    public async Task RemoveFriendAsync(int friendshipId, int userId)
    {
        var friendship = await _friendshipRepository.GetByIdAsync(friendshipId);
        if (friendship == null)
        {
            throw new NotFoundException("Friendship not found");
        }

        if (friendship.User1Id != userId && friendship.User2Id != userId)
        {
            throw new ForbiddenException("You are not part of this friendship");
        }

        await _friendshipRepository.DeleteAsync(friendship);
    }

    public async Task<List<Friendship>> GetFriendsAsync(int userId)
    {
        return await _friendshipRepository.GetByUserAsync(userId, FriendshipStatus.Accepted);
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(int userId)
    {
        return await _friendshipRepository.GetByUserAsync(userId, FriendshipStatus.Pending);
    }
}
