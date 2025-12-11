using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class BlockedUserRepository : IBlockedUserRepository
{
    private readonly UchatDbContext _context;

    public BlockedUserRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<BlockedUser?> GetAsync(int blockerUserId, int blockedUserId)
    {
        return await _context.BlockedUsers
            .FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId);
    }

    public async Task<List<BlockedUser>> GetBlockedByUserAsync(int userId)
    {
        return await _context.BlockedUsers
            .Where(b => b.BlockerUserId == userId)
            .Include(b => b.Blocked)
            .ToListAsync();
    }

    public async Task<List<BlockedUser>> GetBlockersOfUserAsync(int userId)
    {
        return await _context.BlockedUsers
            .Where(b => b.BlockedUserId == userId)
            .Include(b => b.Blocker)
            .ToListAsync();
    }

    public async Task<bool> IsBlockedAsync(int userId1, int userId2)
    {
        return await _context.BlockedUsers
            .AnyAsync(b => 
                (b.BlockerUserId == userId1 && b.BlockedUserId == userId2) ||
                (b.BlockerUserId == userId2 && b.BlockedUserId == userId1));
    }

    public async Task<BlockedUser> CreateAsync(BlockedUser blockedUser)
    {
        blockedUser.BlockedAt = DateTime.UtcNow;
        _context.BlockedUsers.Add(blockedUser);
        await _context.SaveChangesAsync();
        return blockedUser;
    }

    public async Task DeleteAsync(BlockedUser blockedUser)
    {
        _context.BlockedUsers.Remove(blockedUser);
        await _context.SaveChangesAsync();
    }
}
