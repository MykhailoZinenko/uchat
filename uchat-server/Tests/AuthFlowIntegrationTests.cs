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
        Assert.NotNull(result.Data.RefreshToken);
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
        Assert.NotNull(loginResult.Data.RefreshToken);
        Assert.NotEqual(registerResult.Data!.RefreshToken, loginResult.Data.RefreshToken);
    }

    [Fact]
    public async Task Test03_SendMessage_WithValidRefreshToken_ShouldSucceed()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;

        // Act & Assert - Should not throw
        await _connection.InvokeAsync("SendMessage", refreshToken, "Test message");
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

        var refreshToken = registerResult.Data!.RefreshToken;

        // Act
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", 
            refreshToken);

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
        Assert.NotNull(silentLoginResult.Data.RefreshToken);
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
    public async Task Test07_Logout_ShouldRevokeSession()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var refreshToken = registerResult.Data!.RefreshToken;

        // Act
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", 
            refreshToken);

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

        await _connection.InvokeAsync<ApiResponse<bool>>("Logout", refreshToken);

        // Act
        var result = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            refreshToken,
            (string?)null,
            (string?)null);

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
        var refreshToken = registerResult.Data!.RefreshToken;

        // 2. Login (creates 2nd session)
        var loginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login", _testUsername, TestPassword, "Firefox/Windows", (string?)null);
        Assert.True(loginResult.Success);
        refreshToken = loginResult.Data!.RefreshToken;

        // 3. Send message
        await _connection.InvokeAsync("SendMessage", refreshToken, "Test message");

        // 4. Get active sessions (should be 2)
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", refreshToken);
        Assert.Equal(2, sessionsResult.Data!.Count);

        // 5. Silent login (rotate refresh token)
        var silentLoginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", refreshToken, (string?)null, (string?)null);
        Assert.True(silentLoginResult.Success);
        var oldRefreshToken = refreshToken;
        refreshToken = silentLoginResult.Data!.RefreshToken;

        // 6. Verify old refresh token is invalid
        var oldTokenResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", oldRefreshToken, (string?)null, (string?)null);
        Assert.False(oldTokenResult.Success);

        // 7. Send message with rotated refresh token
        await _connection.InvokeAsync("SendMessage", refreshToken, "Message with rotated token");

        // 8. Logout
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", refreshToken);
        Assert.True(logoutResult.Success);

        // 9. Verify refresh token fails after logout
        var afterLogoutRefresh = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", refreshToken, (string?)null, (string?)null);
        Assert.False(afterLogoutRefresh.Success);
    }
}
