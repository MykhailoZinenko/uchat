using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class FriendshipIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername1 = $"friend1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    private readonly string _testUsername2 = $"friend2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
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
    public async Task Test01_SendFriendRequest_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync(_testUsername1);
        var (_, userId2) = await RegisterUserAsync(_testUsername2);

        var result = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(FriendshipStatus.Pending, result.Data.Status);
        Assert.True(result.Data.IsInitiator);
    }

    [Fact]
    public async Task Test02_AcceptFriendRequest_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync($"friend_acc1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"friend_acc2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);
        var friendshipId = sendResult.Data!.Id;

        var acceptResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token2, friendshipId);

        Assert.True(acceptResult.Success);
        Assert.Equal(FriendshipStatus.Accepted, acceptResult.Data!.Status);
    }

    [Fact]
    public async Task Test03_RejectFriendRequest_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync($"friend_rej1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"friend_rej2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);
        var friendshipId = sendResult.Data!.Id;

        var rejectResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "RejectFriendRequest", token2, friendshipId);

        Assert.True(rejectResult.Success);
        Assert.Equal(FriendshipStatus.Rejected, rejectResult.Data!.Status);
    }

    [Fact]
    public async Task Test04_GetFriends_BidirectionalSearch_ShouldWork()
    {
        var (token1, _) = await RegisterUserAsync($"friend_bi1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"friend_bi2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);
        await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token2, sendResult.Data!.Id);

        var friends1 = await _connection!.InvokeAsync<ApiResponse<List<FriendshipDto>>>(
            "GetFriends", token1);
        var friends2 = await _connection!.InvokeAsync<ApiResponse<List<FriendshipDto>>>(
            "GetFriends", token2);

        Assert.True(friends1.Success);
        Assert.True(friends2.Success);
        Assert.Single(friends1.Data!);
        Assert.Single(friends2.Data!);
    }

    [Fact]
    public async Task Test05_RemoveFriend_ShouldSucceed()
    {
        var (token1, _) = await RegisterUserAsync($"friend_rm1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"friend_rm2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);
        await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token2, sendResult.Data!.Id);

        var removeResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "RemoveFriend", token1, sendResult.Data!.Id);

        Assert.True(removeResult.Success);

        var friends1 = await _connection!.InvokeAsync<ApiResponse<List<FriendshipDto>>>("GetFriends", token1);
        Assert.Empty(friends1.Data!);
    }

    [Fact]
    public async Task Test06_GetPendingRequests_ShouldReturnPending()
    {
        var (token1, _) = await RegisterUserAsync($"friend_pend1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUserAsync($"friend_pend2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>("SendFriendRequest", token1, userId2);

        var pending = await _connection!.InvokeAsync<ApiResponse<List<FriendshipDto>>>(
            "GetPendingFriendRequests", token2);

        Assert.True(pending.Success);
        Assert.Single(pending.Data!);
        Assert.Equal(FriendshipStatus.Pending, pending.Data![0].Status);
    }

    [Fact]
    public async Task Test07_SendFriendRequestToSelf_ShouldFail()
    {
        var (token1, userId1) = await RegisterUserAsync($"friend_self_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var result = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test08_AcceptOwnRequest_ShouldFail()
    {
        var (token1, _) = await RegisterUserAsync($"friend_own1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (_, userId2) = await RegisterUserAsync($"friend_own2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);

        var acceptResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token1, sendResult.Data!.Id);

        Assert.False(acceptResult.Success);
    }
}
