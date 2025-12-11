using System.Collections.Generic;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetSessionByIdAsync(int sessionId);
    Task<Session?> GetSessionByTokenAsync(string sessionToken);
    Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId);
    Task<List<Session>> GetExpiredSessionsAsync();
    Task UpdateAsync(Session session);
    Task RevokeAsync(int sessionId);
    Task RevokeAllByUserIdAsync(int userId);
    Task RevokeAllByUserIdExceptAsync(int userId, int sessionIdToKeep);
    Task RevokeByIdsAsync(IEnumerable<int> sessionIds);
    Task DeleteRangeAsync(List<Session> sessions);
}
