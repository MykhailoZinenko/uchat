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
        Assert.NotNull(result.Data.SessionToken);
        Assert.NotEmpty(result.Data.SessionToken);
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
        Assert.NotNull(loginResult.Data.SessionToken);
        Assert.NotEqual(registerResult.Data!.SessionToken, loginResult.Data.SessionToken);
    }

    [Fact]
    public async Task Test03_SendMessage_WithValidSessionToken_ShouldSucceed()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var sessionToken = registerResult.Data!.SessionToken;

        // Act & Assert - Should not throw
        await _connection.InvokeAsync("SendMessage", sessionToken, "Test message");
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

        var sessionToken = registerResult.Data!.SessionToken;

        // Act
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", 
            sessionToken);

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
    public async Task Test05_LoginWithRefreshToken_ShouldNotRotateToken()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var sessionToken = registerResult.Data!.SessionToken;

        // Act
        var silentLoginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            sessionToken, 
            (string?)null, 
            (string?)null);

        // Assert - Токен НЕ должен меняться!
        Assert.True(silentLoginResult.Success);
        Assert.Equal("Tokens refreshed successfully", silentLoginResult.Message);
        Assert.NotNull(silentLoginResult.Data);
        Assert.NotNull(silentLoginResult.Data.SessionToken);
        Assert.Equal(sessionToken, silentLoginResult.Data.SessionToken);
    }

    [Fact]
    public async Task Test06_LoginWithRefreshToken_WithSameToken_ShouldSucceed()
    {
        // Arrange
        var registerResult = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", 
            _testUsername, 
            TestPassword, 
            "Chrome/Windows", 
            (string?)null);
        var sessionToken = registerResult.Data!.SessionToken;

        // Используем токен первый раз
        await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            sessionToken, 
            (string?)null, 
            (string?)null);

        // Act - Используем тот же токен ещё раз
        var result = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            sessionToken, 
            (string?)null, 
            (string?)null);

        // Assert - Должно успешно работать!
        Assert.True(result.Success);
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
        var sessionToken = registerResult.Data!.SessionToken;

        // Act
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", 
            sessionToken);

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
        var sessionToken = registerResult.Data!.SessionToken;

        await _connection.InvokeAsync<ApiResponse<bool>>("Logout", sessionToken);

        // Act
        var result = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", 
            sessionToken,
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
        var sessionToken = registerResult.Data!.SessionToken;

        // 2. Login (creates 2nd session)
        var loginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "Login", _testUsername, TestPassword, "Firefox/Windows", (string?)null);
        Assert.True(loginResult.Success);
        var sessionToken2 = loginResult.Data!.SessionToken;

        // 3. Send message
        await _connection.InvokeAsync("SendMessage", sessionToken, "Test message");

        // 4. Get active sessions (should be 2)
        var sessionsResult = await _connection.InvokeAsync<ApiResponse<List<SessionInfo>>>(
            "GetActiveSessions", sessionToken);
        Assert.Equal(2, sessionsResult.Data!.Count);

        // 5. Silent login - токен НЕ меняется
        var silentLoginResult = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", sessionToken, (string?)null, (string?)null);
        Assert.True(silentLoginResult.Success);
        Assert.Equal(sessionToken, silentLoginResult.Data!.SessionToken);

        // 6. Send message with same token
        await _connection.InvokeAsync("SendMessage", sessionToken, "Message with same token");

        // 7. Logout
        var logoutResult = await _connection.InvokeAsync<ApiResponse<bool>>(
            "Logout", sessionToken);
        Assert.True(logoutResult.Success);

        // 8. Verify token fails after logout
        var afterLogoutRefresh = await _connection.InvokeAsync<ApiResponse<AuthDto>>(
            "LoginWithRefreshToken", sessionToken, (string?)null, (string?)null);
        Assert.False(afterLogoutRefresh.Success);
    }
}
