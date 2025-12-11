using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class PinnedMessageIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername = $"pintest_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
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
    public async Task Test01_PinMessage_ShouldSucceed()
    {
        var sessionToken = await GetSessionTokenAsync();
        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "Message to pin", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        var pinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "PinMessage", sessionToken, roomId, messageId);

        Assert.True(pinResult.Success);
    }

    [Fact]
    public async Task Test02_GetPinnedMessages_ShouldReturnPinned()
    {
        var sessionToken = await GetSessionTokenAsync();
        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "Pinned message", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("PinMessage", sessionToken, roomId, messageId);

        var pinnedResult = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetPinnedMessages", sessionToken, roomId);

        Assert.True(pinnedResult.Success);
        Assert.NotNull(pinnedResult.Data);
        Assert.Contains(pinnedResult.Data, m => m.Id == messageId);
    }

    [Fact]
    public async Task Test03_UnpinMessage_ShouldSucceed()
    {
        var sessionToken = await GetSessionTokenAsync();
        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "To unpin", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("PinMessage", sessionToken, roomId, messageId);
        var unpinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "UnpinMessage", sessionToken, roomId, messageId);

        Assert.True(unpinResult.Success);

        var pinnedResult = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetPinnedMessages", sessionToken, roomId);
        Assert.DoesNotContain(pinnedResult.Data!, m => m.Id == messageId);
    }

    [Fact]
    public async Task Test04_PinSameMessageTwice_ShouldFail()
    {
        var sessionToken = await GetSessionTokenAsync();
        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "Double pin", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("PinMessage", sessionToken, roomId, messageId);
        var secondPin = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "PinMessage", sessionToken, roomId, messageId);

        Assert.False(secondPin.Success);
    }

    [Fact]
    public async Task Test05_UnpinNotPinnedMessage_ShouldFail()
    {
        var sessionToken = await GetSessionTokenAsync();
        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "Not pinned", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        var unpinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "UnpinMessage", sessionToken, roomId, messageId);

        Assert.False(unpinResult.Success);
    }
}
