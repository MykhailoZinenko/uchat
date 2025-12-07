using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface ISessionService
{
    Task<Session> CreateSessionAsync(Session session);
    Task<Session> GetSessionByIdAsync(int sessionId);
    Task<List<Session>> GetActiveSessionsByUserIdAsync(int userId);
    Task UpdateSessionAsync(Session session);
    Task<bool> RevokeSessionAsync(int sessionId);
    Task RevokeAllUserSessionsAsync(int userId);
    Task CleanupExpiredSessionsAsync();
}
