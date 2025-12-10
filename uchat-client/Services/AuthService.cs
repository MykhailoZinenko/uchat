using System;
using System.IO;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Services;

public class AuthService : IAuthService
{
    private readonly IHubConnectionService _hubConnection;
    private string? _sessionToken;
    private string? _username;
    private readonly string _sessionFilePath;

    public string? SessionToken => _sessionToken;
    public string? CurrentUsername => _username;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_sessionToken);

    public AuthService(IHubConnectionService hubConnection)
    {
        _hubConnection = hubConnection;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var uchatFolder = Path.Combine(appDataPath, "uchat");
        Directory.CreateDirectory(uchatFolder);
        _sessionFilePath = Path.Combine(uchatFolder, $"session_{Program.ClientId}.txt");
        Console.WriteLine($"[AuthService] Session file path: {_sessionFilePath}");
    }

    public async Task<ApiResponse<AuthDto>> RegisterAsync(string username, string password)
    {
        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "Register",
            username,
            password,
            GetDeviceInfo(),
            (string?)null // IP will be detected by server
        );

        if (response.Success && response.Data != null)
        {
            SaveSession(response.Data.SessionToken, response.Data.Username);
        }

        return response;
    }

    public async Task<ApiResponse<AuthDto>> LoginAsync(string username, string password)
    {
        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login",
            username,
            password,
            GetDeviceInfo(),
            (string?)null // IP will be detected by server
        );

        if (response.Success && response.Data != null)
        {
            SaveSession(response.Data.SessionToken, response.Data.Username);
        }

        return response;
    }

    public async Task<ApiResponse<AuthDto>> LoginWithRefreshTokenAsync(string refreshToken)
    {
        var response = await _hubConnection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken",
            refreshToken,
            GetDeviceInfo(),
            (string?)null
        );

        if (response.Success && response.Data != null)
        {
            SaveSession(response.Data.SessionToken, response.Data.Username);
        }

        return response;
    }

    public async Task<ApiResponse<bool>> LogoutAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        var response = await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "Logout",
            _sessionToken
        );

        if (response.Success)
        {
            ClearSessionToken();
        }

        return response;
    }

    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync()
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            return new ApiResponse<List<SessionInfo>>
            {
                Success = false,
                Message = "Not logged in"
            };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions",
            _sessionToken
        );
    }

    public async Task<ApiResponse<bool>> RevokeSessionAsync(int sessionId)
    {
        if (string.IsNullOrEmpty(_sessionToken))
        {
            return new ApiResponse<bool> { Success = false, Message = "Not logged in" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "RevokeSession",
            _sessionToken,
            sessionId
        );
    }

    public void SaveSession(string sessionToken, string username)
    {
        _sessionToken = sessionToken;
        _username = username;
        try
        {
            File.WriteAllText(_sessionFilePath, $"{sessionToken}\n{username}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    public string? LoadSessionToken()
    {
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                var lines = File.ReadAllLines(_sessionFilePath);
                if (lines.Length > 0)
                {
                    _sessionToken = lines[0].Trim();
                }
                if (lines.Length > 1)
                {
                    _username = lines[1].Trim();
                }
                return _sessionToken;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load session: {ex.Message}");
        }
        return null;
    }

    public string? LoadUsername()
    {
        if (_username != null) return _username;
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                var lines = File.ReadAllLines(_sessionFilePath);
                if (lines.Length > 1)
                {
                    _username = lines[1].Trim();
                    return _username;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load username: {ex.Message}");
        }
        return null;
    }

    public void ClearSessionToken()
    {
        _sessionToken = null;
        _username = null;
        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear session: {ex.Message}");
        }
    }

    private string GetDeviceInfo()
    {
        return $"{Environment.OSVersion.Platform} - {Environment.MachineName}";
    }
}
