using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class PermissionIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
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

    private async Task<(string token, int userId)> RegisterUser(string username)
    {
        var result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", username, TestPassword, "Test/Windows", (string?)null);
        return (result.Data!.SessionToken, result.Data.UserId);
    }

    // ==================== PINNED MESSAGE PERMISSIONS ====================

    [Fact]
    public async Task Test01_NonMemberCannotPinMessage_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_pin1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"perm_pin2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Private Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, roomId, "Test msg", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        var pinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "PinMessage", token2, roomId, messageId);

        Assert.False(pinResult.Success);
    }

    [Fact]
    public async Task Test02_RegularMemberCannotPinMessage_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_regpin1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_regpin2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("AddRoomMember", token1, roomId, userId2);

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, roomId, "Test msg", (int?)null, (string?)null);
        var messageId = msgResult.Data!.Id;

        var pinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "PinMessage", token2, roomId, messageId);

        Assert.False(pinResult.Success);
    }

    [Fact]
    public async Task Test03_NonMemberCannotUnpinMessage_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_unpin1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"perm_unpin2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Private Room", null);
        var roomId = roomResult.Data!.Id;

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, roomId, "Test msg", (int?)null, (string?)null);
        await _connection!.InvokeAsync<ApiResponse<bool>>("PinMessage", token1, roomId, msgResult.Data!.Id);

        var unpinResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "UnpinMessage", token2, roomId, msgResult.Data!.Id);

        Assert.False(unpinResult.Success);
    }

    // ==================== FRIENDSHIP PERMISSIONS ====================

    [Fact]
    public async Task Test04_CannotAcceptOthersFriendRequest_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_fr1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_fr2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token3, _) = await RegisterUser($"perm_fr3_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);

        var acceptResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token3, sendResult.Data!.Id);

        Assert.False(acceptResult.Success);
    }

    [Fact]
    public async Task Test05_CannotRejectOthersFriendRequest_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_rej1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_rej2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token3, _) = await RegisterUser($"perm_rej3_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);

        var rejectResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "RejectFriendRequest", token3, sendResult.Data!.Id);

        Assert.False(rejectResult.Success);
    }

    [Fact]
    public async Task Test06_CannotRemoveOthersFriendship_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_rm1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_rm2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token3, _) = await RegisterUser($"perm_rm3_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var sendResult = await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "SendFriendRequest", token1, userId2);
        await _connection!.InvokeAsync<ApiResponse<FriendshipDto>>(
            "AcceptFriendRequest", token2, sendResult.Data!.Id);

        var removeResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "RemoveFriend", token3, sendResult.Data!.Id);

        Assert.False(removeResult.Success);
    }

    // ==================== BLOCKED USER PERMISSIONS ====================

    [Fact]
    public async Task Test07_OnlyBlockerCanUnblock_ShouldFail()
    {
        var (token1, userId1) = await RegisterUser($"perm_blk1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_blk2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        await _connection!.InvokeAsync<ApiResponse<bool>>("BlockUser", token1, userId2);

        var unblockResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "UnblockUser", token2, userId1);

        Assert.False(unblockResult.Success);
    }

    // ==================== ROOM PERMISSIONS ====================

    [Fact]
    public async Task Test08_NonOwnerCannotDeleteRoom_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_del1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, userId2) = await RegisterUser($"perm_del2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("AddRoomMember", token1, roomId, userId2);

        var deleteResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "DeleteRoom", token2, roomId);

        Assert.False(deleteResult.Success);
    }

    [Fact]
    public async Task Test09_NonMemberCannotAddMember_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_add1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"perm_add2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (_, userId3) = await RegisterUser($"perm_add3_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var roomResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", token1, "Group", "Test Room", null);
        var roomId = roomResult.Data!.Id;

        var addResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "AddRoomMember", token2, roomId, userId3);

        Assert.False(addResult.Success);
    }

    // ==================== MESSAGE PERMISSIONS ====================

    [Fact]
    public async Task Test10_CannotEditOthersMessage_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_edit1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"perm_edit2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, 1, "My message", (int?)null, (string?)null);

        var editResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "EditMessage", token2, msgResult.Data!.Id, "Hacked");

        Assert.False(editResult.Success);
    }

    [Fact]
    public async Task Test11_CannotDeleteOthersMessage_ShouldFail()
    {
        var (token1, _) = await RegisterUser($"perm_delmsg1_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var (token2, _) = await RegisterUser($"perm_delmsg2_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");

        var msgResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", token1, 1, "My message", (int?)null, (string?)null);

        var deleteResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "DeleteMessage", token2, msgResult.Data!.Id);

        Assert.False(deleteResult.Success);
    }
}
