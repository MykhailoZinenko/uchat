using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface ICryptographyService
{
    string GenerateSessionToken();
    Task<Session> ValidateSessionTokenAsync(string sessionToken);
    long GetSessionLifetimeMs();
}
