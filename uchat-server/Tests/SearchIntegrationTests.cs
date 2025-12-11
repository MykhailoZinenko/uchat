using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class SearchIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername = $"search_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
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

    private async Task<string> GetSessionTokenAsync()
    {
        var result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", _testUsername, TestPassword, "Test/Windows", (string?)null);
        return result.Data!.SessionToken;
    }

    [Fact]
    public async Task Test01_SearchUsers_ShouldReturnResults()
    {
        var sessionToken = await GetSessionTokenAsync();

        var result = await _connection!.InvokeAsync<ApiResponse<List<UserDto>>>(
            "SearchUsers", sessionToken, "search");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data);
    }

    [Fact]
    public async Task Test02_SearchUsers_ShortQuery_ShouldReturnEmpty()
    {
        var sessionToken = await GetSessionTokenAsync();

        var result = await _connection!.InvokeAsync<ApiResponse<List<UserDto>>>(
            "SearchUsers", sessionToken, "a");

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task Test03_SearchMessages_InGlobalRoom_ShouldWork()
    {
        var sessionToken = await GetSessionTokenAsync();
        var uniqueWord = $"unique{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, $"Message with {uniqueWord}", (int?)null, (string?)null);

        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "SearchMessages", sessionToken, uniqueWord, (int?)null, 50);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data!);
        Assert.Contains(result.Data!, m => m.Content.Contains(uniqueWord));
    }

    [Fact]
    public async Task Test04_SearchMessages_InSpecificRoom_ShouldWork()
    {
        var sessionToken = await GetSessionTokenAsync();

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Search Room", null);
        var roomId = roomResult.Data!.Id;

        var uniqueWord = $"roomsearch{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, $"Test {uniqueWord}", (int?)null, (string?)null);

        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "SearchMessages", sessionToken, uniqueWord, roomId, 50);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data!);
        Assert.All(result.Data!, m => Assert.Equal(roomId, m.RoomId));
    }

    [Fact]
    public async Task Test05_SearchMessages_InInaccessibleRoom_ShouldReturnEmpty()
    {
        var (token1, _) = await RegisterUser($"search_acc1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"search_acc2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Private Room", null);
        var roomId = roomResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, roomId, "Secret message", (int?)null, (string?)null);

        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "SearchMessages", token2, "Secret", roomId, 50);

        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    private async Task<(string token, int userId)> RegisterUser(string username)
    {
        var result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", username, TestPassword, "Test/Windows", (string?)null);
        return (result.Data!.SessionToken, result.Data.UserId);
    }
}
