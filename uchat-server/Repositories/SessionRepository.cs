using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly UchatDbContext _context;

    public SessionRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<Session> CreateAsync(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<Session?> GetSessionByIdAsync(int sessionId)
    {
        return await _context.Sessions
            .Where(s => s.ExpiresAt > DateTime.UtcNow)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<Session?> GetSessionByTokenAsync(string sessionToken)
    {
        return await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
    }

    public async Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId)
    {
        return await _context.Sessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Session session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAsync(int sessionId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllByUserIdAsync(int userId)
    {
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllByUserIdExceptAsync(int userId, int sessionIdToKeep)
    {
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && s.Id != sessionIdToKeep)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return;
        }

        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeByIdsAsync(IEnumerable<int> sessionIds)
    {
        var ids = sessionIds.ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var sessions = await _context.Sessions
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return;
        }

        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Session>> GetExpiredSessionsAsync()
    {
        return await _context.Sessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task DeleteRangeAsync(List<Session> sessions)
    {
        _context.Sessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }
}
