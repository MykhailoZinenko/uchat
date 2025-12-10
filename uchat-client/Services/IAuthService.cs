using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Services;

public interface IAuthService
{
    string? SessionToken { get; }
    string? CurrentUsername { get; }
    bool IsAuthenticated { get; }

    Task<ApiResponse<AuthDto>> RegisterAsync(string username, string password);
    Task<ApiResponse<AuthDto>> LoginAsync(string username, string password);
    Task<ApiResponse<AuthDto>> LoginWithRefreshTokenAsync(string refreshToken);
    Task<ApiResponse<bool>> LogoutAsync();
    Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync();
    Task<ApiResponse<bool>> RevokeSessionAsync(int sessionId);

    void SaveSession(string sessionToken, string username);
    string? LoadSessionToken();
    string? LoadUsername();
    void ClearSessionToken();
}
