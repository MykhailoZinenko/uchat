using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_common.Dtos;

namespace uchat_client.Infrastructure.Services.Authentication;

public class AuthService : IAuthService
{
    public event EventHandler? SessionRevoked;

    private readonly IHubConnectionService _hubConnection;
    private readonly ISessionStorageService _sessionStorage;
    private readonly ILoggingService _logger;
    private string? _sessionToken;
    private string? _username;

    public string? SessionToken => _sessionToken;
    public string? CurrentUsername => _username;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionToken);

    public AuthService(
        IHubConnectionService hubConnection,
        ISessionStorageService sessionStorage,
        ILoggingService logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _hubConnection.Connection.On("SessionRevoked", async () => await HandleSessionRevokedAsync());
    }

    public async Task<ApiResponse<AuthDto>> RegisterAsync(string username, string password)
    {
        _logger.LogInformation("Attempting registration for user: {Username}", username);

        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "Register",
            username,
            password,
            _sessionStorage.GetDeviceInfo(),
            null! // IP will be detected by server
        );

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Registration successful for user: {Username}", username);
            SaveSession(response.Data.SessionToken, response.Data.Username);
            await RegisterSessionAsync(response.Data.SessionToken);
        }
        else
        {
            _logger.LogWarning("Registration failed for user {Username}: {Message}", username, response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<AuthDto>> LoginAsync(string username, string password)
    {
        _logger.LogInformation("Attempting login for user: {Username}", username);

        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login",
            username,
            password,
            _sessionStorage.GetDeviceInfo(),
            null! // IP will be detected by server
        );

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Login successful for user: {Username}", username);
            SaveSession(response.Data.SessionToken, response.Data.Username);
            await RegisterSessionAsync(response.Data.SessionToken);
        }
        else
        {
            _logger.LogWarning("Login failed for user {Username}: {Message}", username, response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<AuthDto>> LoginWithRefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting refresh token login");

        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken",
            refreshToken,
            _sessionStorage.GetDeviceInfo(),
            null!
        );

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Refresh token login successful");
            SaveSession(response.Data.SessionToken, response.Data.Username);
            await RegisterSessionAsync(response.Data.SessionToken);
        }
        else
        {
            _logger.LogWarning("Refresh token login failed: {Message}", response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<bool>> LogoutAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _logger.LogWarning("Logout attempted but no active session");
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        _logger.LogInformation("Logging out user: {Username}", _username);

        var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "Logout",
            _sessionToken
        );

        if (response.Success)
        {
            _logger.LogInformation("Logout successful");
            ClearSessionToken();
        }
        else
        {
            _logger.LogWarning("Logout failed: {Message}", response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _logger.LogWarning("GetActiveSessions attempted but no active session");
            return new ApiResponse<List<SessionInfo>>
            {
                Success = false,
                Message = "Not logged in"
            };
        }

        _logger.LogDebug("Fetching active sessions");

        return await _hubConnection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions",
            _sessionToken
        );
    }

    public async Task<ApiResponse<bool>> RevokeSessionAsync(int sessionId)
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _logger.LogWarning("RevokeSession attempted but no active session");
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        _logger.LogInformation("Revoking session: {SessionId}", sessionId);

        var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "RevokeSession",
            _sessionToken,
            sessionId
        );

        if (response.Success)
        {
            _logger.LogInformation("Session {SessionId} revoked successfully", sessionId);
        }
        else
        {
            _logger.LogWarning("Failed to revoke session {SessionId}: {Message}", sessionId, response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<bool>> RevokeSessionsAsync(IEnumerable<int> sessionIds)
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _logger.LogWarning("RevokeSessions attempted but no active session");
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        var ids = sessionIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
        {
            _logger.LogInformation("RevokeSessions called with no session ids, nothing to do");
            return new ApiResponse<bool> { Success = true, Message = "No sessions to revoke", Data = true };
        }

        _logger.LogInformation("Revoking sessions: {SessionIds}", string.Join(',', ids));

        var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "RevokeSessions",
            _sessionToken,
            ids
        );

        if (response.Success)
        {
            _logger.LogInformation("Sessions revoked successfully");
        }
        else
        {
            _logger.LogWarning("Failed to revoke sessions: {Message}", response.Message);
        }

        return response;
    }

    public async Task<ApiResponse<bool>> RevokeAllSessionsAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            _logger.LogWarning("RevokeAllSessions attempted but no active session");
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        _logger.LogInformation("Revoking all sessions for user: {Username}", _username);

        var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "RevokeAllSessions",
            _sessionToken
        );

        if (response.Success)
        {
            _logger.LogInformation("All sessions revoked successfully");
        }
        else
        {
            _logger.LogWarning("Failed to revoke all sessions: {Message}", response.Message);
        }

        return response;
    }

    public void SaveSession(string sessionToken, string username)
    {
        _sessionToken = sessionToken;
        _username = username;

        try
        {
            _sessionStorage.SaveSession(sessionToken, username);
            _logger.LogDebug("Session saved for user: {Username}", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session for user: {Username}", username);
        }
    }

    public string? LoadSessionToken()
    {
        try
        {
            var (token, username) = _sessionStorage.LoadSession();
            _sessionToken = token;
            _username = username;

            if (token != null)
            {
                _logger.LogDebug("Session loaded for user: {Username}", username);
            }

            return _sessionToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session");
            return null;
        }
    }

    public string? LoadUsername()
    {
        if (_username != null) return _username;

        try
        {
            var (_, username) = _sessionStorage.LoadSession();
            _username = username;
            return _username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load username");
            return null;
        }
    }

    public void ClearSessionToken()
    {
        _logger.LogDebug("Clearing session for user: {Username}", _username);
        _sessionToken = null;
        _username = null;

        try
        {
            _sessionStorage.ClearSession();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear session");
        }
    }

    private async Task RegisterSessionAsync(string sessionToken)
    {
        try
        {
            var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
                "RegisterSession",
                sessionToken
            );

            if (!response.Success)
            {
                _logger.LogWarning("Failed to register session for realtime revocation: {Message}", response.Message);
            }
            else
            {
                _logger.LogInformation("Session registered for realtime revocation notifications");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RegisterSession invocation failed");
        }
    }

    private async Task HandleSessionRevokedAsync()
    {
        _logger.LogWarning("Session revoked remotely. Logging out.");
        ClearSessionToken();
        SessionRevoked?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }
}
