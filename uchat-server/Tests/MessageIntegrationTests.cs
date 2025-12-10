using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_server.Tests;

public class MessageIntegrationTests : IAsyncLifetime
{
    private HubConnection? _connection;
    private const string HubUrl = "http://localhost:5000/chat";
    private readonly string _testUsername = $"msgtest_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
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

    // ==================== POSITIVE TESTS ====================

    [Fact]
    public async Task Test01_SendMessage_ToGlobalRoom_ShouldSucceed()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Hello from test!", (int?)null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.RoomId);
        Assert.Equal("Hello from test!", result.Data.Content);
        Assert.Equal(MessageType.Text, result.Data.MessageType);
        Assert.Null(result.Data.ServiceAction);
    }

    [Fact]
    public async Task Test02_SendMessage_ToGroupRoom_AsMember_ShouldSucceed()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();
        var createResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Group", null);
        var roomId = createResult.Data!.Id;

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, roomId, "Message in group", (int?)null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(roomId, result.Data.RoomId);
        Assert.Equal("Message in group", result.Data.Content);
    }

    [Fact]
    public async Task Test03_SendMessage_WithReply_ShouldSucceed()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();
        var firstMessage = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Original message", (int?)null);
        var messageId = firstMessage.Data!.Id;

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "This is a reply", messageId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(messageId, result.Data.ReplyToMessageId);
    }

    [Fact]
    public async Task Test04_GetMessages_ShouldReturnMessages()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();
        await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Test message for get", (int?)null);

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 1, 50, (int?)null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data);
    }

    [Fact]
    public async Task Test05_GetMessages_WithPagination_ShouldWork()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();
        
        // Send multiple messages
        for (int i = 0; i < 5; i++)
        {
            await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
                "SendMessage", sessionToken, 1, $"Message {i}", (int?)null);
        }

        // Get messages with limit
        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 1, 3, (int?)null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.Count <= 3);
    }

    // ==================== NEGATIVE TESTS ====================

    [Fact]
    public async Task Test06_SendMessage_ToRoomNotMember_ShouldFail()
    {
        // Arrange - Create room with one user
        var sessionToken1 = await GetSessionTokenAsync();
        var createResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken1, "Group", "Private Room", null);
        var roomId = createResult.Data!.Id;

        // Register another user
        var user2Username = $"msgtest_other_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var user2Result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", user2Username, TestPassword, "Test/Windows", (string?)null);
        var sessionToken2 = user2Result.Data!.SessionToken;

        // Act - Try to send message as non-member
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken2, roomId, "Should fail", (int?)null);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test07_SendMessage_EmptyContent_ShouldFail()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "", (int?)null);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test08_SendMessage_WhitespaceOnly_ShouldFail()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();

        // Act
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "   ", (int?)null);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test09_GetMessages_FromRoomNotMember_ShouldFail()
    {
        // Arrange - Create room with one user
        var sessionToken1 = await GetSessionTokenAsync();
        var createResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken1, "Group", "Private Room", null);
        var roomId = createResult.Data!.Id;

        // Register another user
        var user2Username = $"msgtest_get_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var user2Result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", user2Username, TestPassword, "Test/Windows", (string?)null);
        var sessionToken2 = user2Result.Data!.SessionToken;

        // Act - Try to get messages as non-member
        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken2, roomId, 50, (int?)null);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test10_SendMessage_InvalidReplyId_ShouldFail()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();

        // Act - Reply to non-existent message
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Invalid reply", 999999);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test11_SendMessage_ReplyToDifferentRoom_ShouldFail()
    {
        // Arrange
        var sessionToken = await GetSessionTokenAsync();
        
        // Create a group room and send a message there
        var createResult = await _connection!.InvokeAsync<ApiResponse<RoomDto>>(
            "CreateRoom", sessionToken, "Group", "Test Group", null);
        var groupRoomId = createResult.Data!.Id;
        
        var groupMessage = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, groupRoomId, "Group message", (int?)null);
        var groupMessageId = groupMessage.Data!.Id;

        // Act - Try to reply to group message from global room
        var result = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Reply to wrong room", groupMessageId);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test12_GetMessages_NonExistentRoom_ShouldFail()
    {
        var sessionToken = await GetSessionTokenAsync();

        var result = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 999999, 50, (int?)null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Test13_EditMessage_ShouldSucceed()
    {
        var sessionToken = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Original content", (int?)null);
        var messageId = sendResult.Data!.Id;

        var editResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "EditMessage", sessionToken, messageId, "Edited content");

        Assert.True(editResult.Success);

        var messages = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 1, 50, (int?)null);
        var editedMessage = messages.Data!.FirstOrDefault(m => m.Id == messageId);

        Assert.NotNull(editedMessage);
        Assert.Equal("Edited content", editedMessage.Content);
        Assert.True(editedMessage.IsEdited);
        Assert.NotNull(editedMessage.EditedAt);
    }

    [Fact]
    public async Task Test14_DeleteMessage_ShouldSucceed()
    {
        var sessionToken = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Message to delete", (int?)null);
        var messageId = sendResult.Data!.Id;

        var deleteResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "DeleteMessage", sessionToken, messageId);

        Assert.True(deleteResult.Success);

        var messages = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 1, 50, (int?)null);
        var deletedMessage = messages.Data!.FirstOrDefault(m => m.Id == messageId);

        Assert.Null(deletedMessage);
    }

    [Fact]
    public async Task Test15_EditOtherUserMessage_ShouldFail()
    {
        var sessionToken1 = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken1, 1, "Original message", (int?)null);
        var messageId = sendResult.Data!.Id;

        var user2Username = $"msgtest_edit_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var user2Result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", user2Username, TestPassword, "Test/Windows", (string?)null);
        var sessionToken2 = user2Result.Data!.SessionToken;

        var editResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "EditMessage", sessionToken2, messageId, "Hacked content");

        Assert.False(editResult.Success);
    }

    [Fact]
    public async Task Test16_DeleteOtherUserMessage_ShouldFail()
    {
        var sessionToken1 = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken1, 1, "My message", (int?)null);
        var messageId = sendResult.Data!.Id;

        var user2Username = $"msgtest_del_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var user2Result = await _connection!.InvokeAsync<ApiResponse<AuthDto>>(
            "Register", user2Username, TestPassword, "Test/Windows", (string?)null);
        var sessionToken2 = user2Result.Data!.SessionToken;

        var deleteResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "DeleteMessage", sessionToken2, messageId);

        Assert.False(deleteResult.Success);
    }

    [Fact]
    public async Task Test17_EditDeletedMessage_ShouldFail()
    {
        var sessionToken = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Will be deleted", (int?)null);
        var messageId = sendResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("DeleteMessage", sessionToken, messageId);

        var editResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "EditMessage", sessionToken, messageId, "Try to edit");

        Assert.False(editResult.Success);
    }

    [Fact]
    public async Task Test18_DeleteAlreadyDeletedMessage_ShouldFail()
    {
        var sessionToken = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Delete me twice", (int?)null);
        var messageId = sendResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("DeleteMessage", sessionToken, messageId);
        var deleteResult = await _connection!.InvokeAsync<ApiResponse<bool>>(
            "DeleteMessage", sessionToken, messageId);

        Assert.False(deleteResult.Success);
    }

    [Fact]
    public async Task Test19_MultipleEdits_ShouldShowLatestContent()
    {
        var sessionToken = await GetSessionTokenAsync();
        var sendResult = await _connection!.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage", sessionToken, 1, "Version 1", (int?)null);
        var messageId = sendResult.Data!.Id;

        await _connection!.InvokeAsync<ApiResponse<bool>>("EditMessage", sessionToken, messageId, "Version 2");
        await _connection!.InvokeAsync<ApiResponse<bool>>("EditMessage", sessionToken, messageId, "Version 3");
        await _connection!.InvokeAsync<ApiResponse<bool>>("EditMessage", sessionToken, messageId, "Version 4");

        var messages = await _connection!.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages", sessionToken, 1, 50, (int?)null);
        var editedMessage = messages.Data!.FirstOrDefault(m => m.Id == messageId);

        Assert.NotNull(editedMessage);
        Assert.Equal("Version 4", editedMessage.Content);
        Assert.True(editedMessage.IsEdited);
    }
}

