using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IAuthService
{
    event EventHandler? SessionRevoked;
    string? SessionToken { get; }
    string? CurrentUsername { get; }
    int? CurrentUserId { get; }
    bool IsAuthenticated { get; }

    Task<ApiResponse<AuthDto>> RegisterAsync(string username, string password);
    Task<ApiResponse<AuthDto>> LoginAsync(string username, string password);
    Task<ApiResponse<AuthDto>> LoginWithRefreshTokenAsync(string refreshToken);
    Task<ApiResponse<bool>> LogoutAsync();
    Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync();
    Task<ApiResponse<bool>> RevokeSessionAsync(int sessionId);
    Task<ApiResponse<bool>> RevokeSessionsAsync(IEnumerable<int> sessionIds);
    Task<ApiResponse<bool>> RevokeAllSessionsAsync();

    void SaveSession(string sessionToken, int userId, string username);
    string? LoadSessionToken();
    string? LoadUsername();
    int? LoadUserId();
    void ClearSessionToken();
}
