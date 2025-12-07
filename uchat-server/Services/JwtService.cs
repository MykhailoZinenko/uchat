using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using uchat_server.Configuration;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Models;

namespace uchat_server.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly ISessionService _sessionService;

    public JwtService(IOptions<JwtSettings> options, ISessionService sessionService)
    {
        _settings = options.Value;
        _sessionService = sessionService;
    }

    public long GetRefreshTokenLifetimeMs() => _settings.RefreshTokenLifetimeMs;

    public string GenerateAccessToken(AccessTokenPayload payload)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.AccessSecretKey);

        var claims = new[]
        {
            new Claim("userId", payload.UserId.ToString()),
            new Claim("sessionId", payload.SessionId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMilliseconds(_settings.AccessTokenLifetimeMs),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(RefreshTokenPayload payload)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.RefreshSecretKey);

        var claims = new[]
        {
            new Claim("userId", payload.UserId.ToString()),
            new Claim("sessionId", payload.SessionId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMilliseconds(_settings.RefreshTokenLifetimeMs),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<AccessTokenPayload> ValidateAccessTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.AccessSecretKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userIdClaim = principal.FindFirst("userId");
            var sessionIdClaim = principal.FindFirst("sessionId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) &&
                sessionIdClaim != null && int.TryParse(sessionIdClaim.Value, out int sessionId))
            {
                Session session = await _sessionService.GetSessionByIdAsync(sessionId);

                return new AccessTokenPayload(userId, sessionId);
            }

            throw new ValidateAccessTokenException("Invalid token claims");
        }
        catch (ValidateAccessTokenException)
        {
            throw;
        }
        catch
        {
            throw new ValidateAccessTokenException("Invalid token");
        }
    }

    public async Task<RefreshTokenPayload> ValidateRefreshTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.RefreshSecretKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var userIdClaim = principal.FindFirst("userId");
            var sessionIdClaim = principal.FindFirst("sessionId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) &&
                sessionIdClaim != null && int.TryParse(sessionIdClaim.Value, out int sessionId))
            {
                Session session = await _sessionService.GetSessionByIdAsync(sessionId);

                return new RefreshTokenPayload(userId, sessionId);
            }

            throw new ValidateRefreshTokenException("Invalid token claims");
        }
        catch (ValidateRefreshTokenException)
        {
            throw;
        }
        catch
        {
            throw new ValidateRefreshTokenException("Invalid token");
        }
    }
}
