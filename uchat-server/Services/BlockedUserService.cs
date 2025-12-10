using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class BlockedUserService : IBlockedUserService
{
    private readonly IBlockedUserRepository _blockedUserRepository;
    private readonly IUserRepository _userRepository;

    public BlockedUserService(IBlockedUserRepository blockedUserRepository, IUserRepository userRepository)
    {
        _blockedUserRepository = blockedUserRepository;
        _userRepository = userRepository;
    }

    public async Task<BlockedUser> BlockUserAsync(int blockerId, int blockedUserId)
    {
        if (blockerId == blockedUserId)
        {
            throw new ValidationException("Cannot block yourself");
        }

        var blockedUser = await _userRepository.GetByIdAsync(blockedUserId);
        if (blockedUser == null)
        {
            throw new NotFoundException("User not found");
        }

        var existing = await _blockedUserRepository.GetAsync(blockerId, blockedUserId);
        if (existing != null)
        {
            throw new ValidationException("User is already blocked");
        }

        var block = new BlockedUser
        {
            BlockerUserId = blockerId,
            BlockedUserId = blockedUserId
        };

        return await _blockedUserRepository.CreateAsync(block);
    }

    public async Task UnblockUserAsync(int blockerId, int blockedUserId)
    {
        var block = await _blockedUserRepository.GetAsync(blockerId, blockedUserId);
        if (block == null)
        {
            throw new NotFoundException("User is not blocked");
        }

        await _blockedUserRepository.DeleteAsync(block);
    }

    public async Task<List<BlockedUser>> GetBlockedUsersAsync(int userId)
    {
        return await _blockedUserRepository.GetBlockedByUserAsync(userId);
    }

    public async Task<List<BlockedUser>> GetBlockersOfUserAsync(int userId)
    {
        return await _blockedUserRepository.GetBlockersOfUserAsync(userId);
    }

    public async Task<bool> IsBlockedAsync(int userId1, int userId2)
    {
        return await _blockedUserRepository.IsBlockedAsync(userId1, userId2);
    }
}
