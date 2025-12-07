using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace uchat_server.Tests;

public class AuthFlowIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername = $"testuser_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    private const string TestPassword = "password123";

    public async Task InitializeAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl)
            .WithAutomaticReconnect()
            .Build();

        await _connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Test01_Register_ShouldReturnTokens()
    {
        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Account created successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
    }

    [Fact]
    public async Task Test02_Login_ShouldReturnTokens()
    {
        // Arrange - Register first
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);

        // Act
        var loginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login", 
            _testUsername, 
            TestPassword, 
            "Firefox/Windows", 
            (string?)null);

        // Assert
        Assert.True(loginResult.Success);
        Assert.Equal("Logged in successfully", loginResult.Message);
        Assert.NotNull(loginResult.Data);
        Assert.NotNull(loginResult.Data.AccessToken);
        Assert.NotNull(loginResult.Data.RefreshToken);
        Assert.NotEqual(registerResult.Data!.AccessToken, loginResult.Data.AccessToken);
    }

    [Fact]
    public async Task Test03_SendMessage_WithValidAccessToken_ShouldSucceed()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var accessToken = registerResult.Data!.AccessToken;

        // Act & Assert - Should not throw
        await _connection.InvokeAsync("SendMessage", accessToken, "Test message");
    }

    [Fact]
    public async Task Test04_GetActiveSessions_ShouldReturnTwoSessions()
    {
        // Arrange - Register and Login to create 2 sessions
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        
        await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login", 
            _testUsername, 
            TestPassword, 
            "Firefox/Windows", 
            (string?)null);

        var accessToken = registerResult.Data!.AccessToken;

        // Act
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", 
            accessToken);

        // Assert
        Assert.True(sessionsResult.Success);
        Assert.NotNull(sessionsResult.Data);
        Assert.Equal(2, sessionsResult.Data.Count);
        Assert.All(sessionsResult.Data, session =>
        {
            Assert.NotNull(session.DeviceInfo);
            Assert.NotEqual(default, session.CreatedAt);
            Assert.NotEqual(default, session.LastActivityAt);
            Assert.NotEqual(default, session.ExpiresAt);
        });
    }

    [Fact]
    public async Task Test05_LoginWithRefreshToken_ShouldRotateTokens()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;
        var accessToken = registerResult.Data.AccessToken;

        // Act
        var silentLoginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            refreshToken, 
            (string?)null, 
            (string?)null);

        // Assert
        Assert.True(silentLoginResult.Success);
        Assert.Equal("Tokens refreshed successfully", silentLoginResult.Message);
        Assert.NotNull(silentLoginResult.Data);
        Assert.NotNull(silentLoginResult.Data.AccessToken);
        Assert.NotNull(silentLoginResult.Data.RefreshToken);
        Assert.NotEqual(accessToken, silentLoginResult.Data.AccessToken);
        Assert.NotEqual(refreshToken, silentLoginResult.Data.RefreshToken);
    }

    [Fact]
    public async Task Test06_LoginWithRefreshToken_WithOldToken_ShouldFail()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var oldRefreshToken = registerResult.Data!.RefreshToken;

        // Rotate tokens
        await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            oldRefreshToken, 
            (string?)null, 
            (string?)null);

        // Act
        var result = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            oldRefreshToken, 
            (string?)null, 
            (string?)null);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or expired refresh token", result.Message);
    }

    [Fact]
    public async Task Test07_RefreshAccessToken_ShouldReturnNewAccessToken()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;
        var oldAccessToken = registerResult.Data.AccessToken;

        // Act
        var refreshResult = await _connection.InvokeAsync<ApiResponse<string>>(
            "RefreshAccessToken", 
            refreshToken);

        // Assert
        Assert.True(refreshResult.Success);
        Assert.Equal("Access token refreshed successfully", refreshResult.Message);
        Assert.NotNull(refreshResult.Data);
        Assert.NotEqual(oldAccessToken, refreshResult.Data);
    }

    [Fact]
    public async Task Test08_SendMessage_WithRefreshedAccessToken_ShouldSucceed()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;

        var refreshResult = await _connection.InvokeAsync<ApiResponse<string>>(
            "RefreshAccessToken", 
            refreshToken);
        var newAccessToken = refreshResult.Data!;

        // Act & Assert - Should not throw
        await _connection.InvokeAsync("SendMessage", newAccessToken, "Message with refreshed token");
    }

    [Fact]
    public async Task Test09_Logout_ShouldRevokeSession()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var accessToken = registerResult.Data!.AccessToken;

        // Act
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", 
            accessToken);

        // Assert
        Assert.True(logoutResult.Success);
        Assert.Equal("Logged out successfully", logoutResult.Message);
        Assert.True(logoutResult.Data);
    }

    [Fact]
    public async Task Test10_RefreshAccessToken_AfterLogout_ShouldFail()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;
        var accessToken = registerResult.Data.AccessToken;

        await _connection.InvokeAsync<ApiResponse<bool>>("Logout", accessToken);

        // Act
        var result = await _connection.InvokeAsync<ApiResponse<string>>(
            "RefreshAccessToken", 
            refreshToken);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CompleteAuthFlow_Integration_Test()
    {
        // 1. Register
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", _testUsername, TestPassword, "Chrome/Windows", (string?)null);
        Assert.True(registerResult.Success);
        var accessToken = registerResult.Data!.AccessToken;
        var refreshToken = registerResult.Data.RefreshToken;

        // 2. Login (creates 2nd session)
        var loginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login", _testUsername, TestPassword, "Firefox/Windows", (string?)null);
        Assert.True(loginResult.Success);
        accessToken = loginResult.Data!.AccessToken;
        refreshToken = loginResult.Data.RefreshToken;

        // 3. Send message
        await _connection.InvokeAsync("SendMessage", accessToken, "Test message");

        // 4. Get active sessions (should be 2)
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", accessToken);
        Assert.Equal(2, sessionsResult.Data!.Count);

        // 5. Silent login (rotate both tokens)
        var silentLoginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", refreshToken, (string?)null, (string?)null);
        Assert.True(silentLoginResult.Success);
        var oldRefreshToken = refreshToken;
        accessToken = silentLoginResult.Data!.AccessToken;
        refreshToken = silentLoginResult.Data.RefreshToken;

        // 6. Verify old refresh token is invalid
        var oldTokenResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", oldRefreshToken, (string?)null, (string?)null);
        Assert.False(oldTokenResult.Success);

        // 7. Refresh access token only
        var refreshAccessResult = await _connection.InvokeAsync<ApiResponse<string>>(
            "RefreshAccessToken", refreshToken);
        Assert.True(refreshAccessResult.Success);
        var newAccessToken = refreshAccessResult.Data!;

        // 8. Send message with new access token
        await _connection.InvokeAsync("SendMessage", newAccessToken, "Message with refreshed token");

        // 9. Logout
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", newAccessToken);
        Assert.True(logoutResult.Success);

        // 10. Verify refresh token fails after logout
        var afterLogoutRefresh = await _connection.InvokeAsync<ApiResponse<string>>(
            "RefreshAccessToken", refreshToken);
        Assert.False(afterLogoutRefresh.Success);
    }
}
