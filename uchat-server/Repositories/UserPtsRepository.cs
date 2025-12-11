using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Repositories.Interfaces;

namespace uchat_server.Repositories;

public class UserPtsRepository : IUserPtsRepository
{
    private readonly UchatDbContext _context;

    public UserPtsRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<UserPts?> GetByUserIdAsync(int userId)
    {
        return await _context.UserPts
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<int> GetCurrentPtsAsync(int userId)
    {
        var userPts = await GetByUserIdAsync(userId);
        if (userPts == null)
        {
            await InitializeAsync(userId);
            return 0;
        }
        return userPts.CurrentPts;
    }

    public async Task<int> IncrementPtsAsync(int userId)
    {
        var userPts = await GetByUserIdAsync(userId);
        if (userPts == null)
        {
            await InitializeAsync(userId);
            userPts = await GetByUserIdAsync(userId);
        }

        if (userPts != null)
        {
            userPts.CurrentPts++;
            userPts.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return userPts.CurrentPts;
        }

        return 0;
    }

    public async Task InitializeAsync(int userId)
    {
        var exists = await _context.UserPts.AnyAsync(p => p.UserId == userId);
        if (!exists)
        {
            _context.UserPts.Add(new UserPts
            {
                UserId = userId,
                CurrentPts = 0,
                LastUpdated = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
