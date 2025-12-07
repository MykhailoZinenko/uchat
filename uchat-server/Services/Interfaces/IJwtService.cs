using uchat_server.Models;

namespace uchat_server.Services;

public interface IJwtService
{
    string GenerateRefreshToken(RefreshTokenPayload payload);
    Task<RefreshTokenPayload> ValidateRefreshTokenAsync(string token);
    long GetRefreshTokenLifetimeMs();
}
