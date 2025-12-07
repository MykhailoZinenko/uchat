using uchat_common.Dtos;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IAuthService
{
    Task<AuthDto> RegisterAsync(string username, string password, string deviceInfo, string? ipAddress = null, string? email = null);
    Task<AuthDto> LoginAsync(string username, string password, string deviceInfo, string? ipAddress = null);
    Task<AuthDto?> LoginWithRefreshTokenAsync(string refreshToken, string? deviceInfo = null, string? ipAddress = null);
}
