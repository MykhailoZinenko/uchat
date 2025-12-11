using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;

    public SessionService(ISessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        return await _sessionRepository.CreateAsync(session);
    }

    public async Task<Session> GetSessionByIdAsync(int sessionId)
    {
        Session? session = await _sessionRepository.GetSessionByIdAsync(sessionId);
        if (session == null)
        {
            throw new NotFoundException("Session not found");
        }
        return session;
    }

    public async Task<Session?> GetSessionByTokenAsync(string sessionToken)
    {
        return await _sessionRepository.GetSessionByTokenAsync(sessionToken);
    }

    public async Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId)
    {
        return await _sessionRepository.GetActiveSessionsByUserIdAsync(userId);
    }

    public async Task UpdateSessionAsync(Session session)
    {
        await _sessionRepository.UpdateAsync(session);
    }

    public async Task<bool> RevokeSessionAsync(int sessionId)
    {
        await _sessionRepository.RevokeAsync(sessionId);
        return true;
    }

    public async Task RevokeAllUserSessionsAsync(int userId)
    {
        await _sessionRepository.RevokeAllByUserIdAsync(userId);
    }

    public async Task RevokeAllUserSessionsExceptAsync(int userId, int sessionIdToKeep)
    {
        await _sessionRepository.RevokeAllByUserIdExceptAsync(userId, sessionIdToKeep);
    }

    public async Task<bool> RevokeSessionsAsync(IEnumerable<int> sessionIds)
    {
        await _sessionRepository.RevokeByIdsAsync(sessionIds);
        return true;
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _sessionRepository.GetExpiredSessionsAsync();
        if (expiredSessions.Any())
        {
            await _sessionRepository.DeleteRangeAsync(expiredSessions);
        }
    }
}
