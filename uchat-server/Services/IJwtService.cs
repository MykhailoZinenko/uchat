using uchat_server.Models;

namespace uchat_server.Services;

public interface IJwtService
{
    string GenerateAccessToken(AccessTokenPayload payload);
    string GenerateRefreshToken(RefreshTokenPayload payload);
    Task<AccessTokenPayload> ValidateAccessTokenAsync(string token);
    Task<RefreshTokenPayload> ValidateRefreshTokenAsync(string token);
    int GetRefreshTokenLifetimeMs();
}
