using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Repositories.Interfaces;

namespace uchat_server.Repositories;

public class UserUpdateRepository : IUserUpdateRepository
{
    private readonly UchatDbContext _context;

    public UserUpdateRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserUpdate>> GetUpdatesAsync(int userId, int fromPts, int limit = 100)
    {
        return await _context.UserUpdates
            .Where(u => u.UserId == userId && u.Pts > fromPts)
            .OrderBy(u => u.Pts)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<UserUpdate> CreateUpdateAsync(int userId, int pts, string updateType, string updateData)
    {
        var update = new UserUpdate
        {
            UserId = userId,
            Pts = pts,
            UpdateType = updateType,
            UpdateData = updateData,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserUpdates.Add(update);
        await _context.SaveChangesAsync();
        return update;
    }

    public async Task CleanupOldUpdatesAsync(DateTime before)
    {
        var oldUpdates = await _context.UserUpdates
            .Where(u => u.CreatedAt < before)
            .ToListAsync();

        _context.UserUpdates.RemoveRange(oldUpdates);
        await _context.SaveChangesAsync();
    }
}
