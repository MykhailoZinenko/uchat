using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public class ChatService
{
    private readonly UchatDbContext _db;

    public ChatService(UchatDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateUserAsync(string username, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public bool VerifyPassword(User user, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<List<Message>> GetRecentMessagesAsync(int limit = 50)
    {
        return await _db.Messages
            .Include(m => m.Sender)
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message> SaveMessageAsync(int senderId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await _db.Entry(message).Reference(m => m.Sender).LoadAsync();

        return message;
    }

    public async Task UpdateUserLastSeenAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastSeenAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<Session> CreateSessionAsync(int userId, string deviceInfo, int expirationDays = 30)
    {
        var token = GenerateSecureToken();

        var session = new Session
        {
            Token = token,
            UserId = userId,
            DeviceInfo = deviceInfo,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            LastActivityAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        return session;
    }

    public async Task<(bool IsValid, User? User)> ValidateSessionAsync(string token)
    {
        var session = await _db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token);

        if (session == null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
        {
            return (false, null);
        }

        session.LastActivityAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, session.User);
    }

    public async Task<List<Session>> GetUserSessionsAsync(int userId)
    {
        return await _db.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();
    }

    public async Task<bool> RevokeSessionAsync(string token)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session == null)
            return false;

        session.IsRevoked = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task RevokeAllUserSessionsAsync(int userId)
    {
        var sessions = await _db.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
        }

        await _db.SaveChangesAsync();
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _db.Sessions
            .Where(s => s.ExpiresAt < DateTime.UtcNow || s.IsRevoked)
            .ToListAsync();

        _db.Sessions.RemoveRange(expiredSessions);
        await _db.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
