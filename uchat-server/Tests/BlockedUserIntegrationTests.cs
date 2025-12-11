using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class BlockedUserIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername1 = $"block1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    private readonly string _testUsername2 = $"block2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
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

    private async Task<(string token, int userId)> RegisterUserAsync(string username)
    {
        var result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", username, TestPassword, "Test/Windows", (string?)null);
        return (result.Data!.SessionToken, result.Data.UserId);
    }

    [Fact]
    public async Task Test01_BlockUser_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync(_testUsername1);
        var (_, userId2) = await RegisterUserAsync(_testUsername2);

        var result = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "BlockUser", token1, userId2);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Test02_GetBlockedUsers_ShouldReturnBlocked()
    {
        var (token1, _) = await RegisterUserAsync($"block_get1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (_, userId2) = await RegisterUserAsync($"block_get2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var result = await _connection!.InvokeAsync<ApiResponse<List<UserDto>>>(
            "GetBlockedUsers", token1);

        Assert.True(result.Success);
        Assert.Contains(result.Data!, u => u.Id == userId2);
    }

    [Fact]
    public async Task Test03_GetBlockersOfMe_ShouldReturnBlockers()
    {
        var (token1, _) = await RegisterUserAsync($"block_of1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"block_of2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var result = await _connection!.InvokeAsync<ApiResponse<List<UserDto>>>(
            "GetBlockersOfMe", token2);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data!);
    }

    [Fact]
    public async Task Test04_UnblockUser_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync($"unblock1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (_, userId2) = await RegisterUserAsync($"unblock2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);
        var result = await _connection!.InvokeAsync<ApiResponse<bool>>("UnblockUser", token1, userId2);

        Assert.True(result.Success);

        var blocked = await _connection!.InvokeAsync<ApiResponse<List<UserDto>>>("GetBlockedUsers", token1);
        Assert.DoesNotContain(blocked.Data!, u => u.Id == userId2);
    }

    [Fact]
    public async Task Test05_BlockSelf_ShouldFail()
    {
        var (token1, userId1) = await RegisterUserAsync($"block_self_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var result = await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test06_BlockedUserCannotMessage_ShouldFail()
    {
        var (token1, userId1) = await RegisterUserAsync($"block_msg1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"block_msg2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Direct", null, null);
        var roomId = roomResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("AddRoomMember", token1, roomId, userId2);

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token2, roomId, "Should fail", (int?)null);

        Assert.False(msgResult.Success);
        Assert.Contains("blocked", msgResult.Message?.ToLower() ?? "");
    }

    [Fact]
    public async Task Test07_BlockerCannotMessageBlocked_ShouldFail()
    {
        var (token1, userId1) = await RegisterUserAsync($"block_rev1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"block_rev2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Direct", null, null);
        var roomId = roomResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("AddRoomMember", token1, roomId, userId2);

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, roomId, "Blocker msg", (int?)null);

        Assert.False(msgResult.Success);
        Assert.Contains("blocked", msgResult.Message?.ToLower() ?? "");
    }

    [Fact]
    public async Task Test08_UnblockByNonBlocker_ShouldFail()
    {
        var (token1, userId1) = await RegisterUserAsync($"unblock_other1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"unblock_other2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var result = await _connection!.InvokeAsync<ApiResponse<bool>>("UnblockUser", token2, userId1);

        Assert.False(result.Success);
    }
}
