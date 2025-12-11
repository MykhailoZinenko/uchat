using Microsoft.EntityFrameworkCore;
using uchat_common.Enums;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly UchatDbContext _context;

    public FriendshipRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<Friendship?> GetByIdAsync(int id)
    {
        return await _context.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Friendship?> GetByUsersAsync(int userId1, int userId2)
    {
        return await _context.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .FirstOrDefaultAsync(f => 
                (f.User1Id == userId1 && f.User2Id == userId2) ||
                (f.User1Id == userId2 && f.User2Id == userId1));
    }

    public async Task<List<Friendship>> GetByUserAsync(int userId, FriendshipStatus? status = null)
    {
        var query = _context.Friendships
            .Include(f => f.User1)
            .Include(f => f.User2)
            .Where(f => f.User1Id == userId || f.User2Id == userId);

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    public async Task<Friendship> CreateAsync(Friendship friendship)
    {
        friendship.CreatedAt = DateTime.UtcNow;
        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return friendship;
    }

    public async Task UpdateAsync(Friendship friendship)
    {
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Friendship friendship)
    {
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
    }
}
