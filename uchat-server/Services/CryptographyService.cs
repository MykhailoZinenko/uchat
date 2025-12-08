using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using uchat_server.Configuration;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;

namespace uchat_server.Services;

public class CryptographyService : ICryptographyService
{
    private readonly SessionSettings _settings;
    private readonly ISessionService _sessionService;

    public CryptographyService(IOptions<SessionSettings> options, ISessionService sessionService)
    {
        _settings = options.Value;
        _sessionService = sessionService;
    }

    public long GetSessionLifetimeMs() => _settings.SessionTokenLifetimeMs;

    public string GenerateSessionToken()
    {
        byte[] randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return Convert.ToHexString(randomBytes).ToLower();
    }

    public async Task<Session> ValidateSessionTokenAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new ValidateRefreshTokenException("Session token is required");
        }

        var session = await _sessionService.GetSessionByTokenAsync(sessionToken);

        if (session == null)
        {
            throw new ValidateRefreshTokenException("Invalid session token");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            throw new ValidateRefreshTokenException("Session has expired");
        }

        session.ExpiresAt = DateTime.UtcNow.AddMilliseconds(_settings.SessionTokenLifetimeMs);
        session.LastActivityAt = DateTime.UtcNow;
        await _sessionService.UpdateSessionAsync(session);

        return session;
    }
}
