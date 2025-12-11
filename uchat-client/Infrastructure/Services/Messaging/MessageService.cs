using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_common.Dtos;

namespace uchat_client.Infrastructure.Services.Messaging;

public class MessageService : IMessageService
{
    private readonly IHubConnectionService _hubConnection;
    private readonly ILoggingService _logger;

    public event EventHandler<MessageDto>? MessageReceived;
    public event EventHandler<(int messageId, string newContent)>? MessageEdited;
    public event EventHandler<int>? MessageDeleted;
    public event EventHandler<MessageAckDto>? MessageAcknowledged;
    public event EventHandler<RoomDto>? RoomUpdated;
    public event EventHandler<int>? RoomJoined;
    public event EventHandler<int>? RoomLeft;

    private readonly IAuthService _authService;

    public MessageService(
        IHubConnectionService hubConnection,
        IAuthService authService,
        ILoggingService logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _hubConnection.Connection.On<MessageDto>("MessageReceived", message =>
        {
            _logger.LogDebug("MessageReceived event: RoomId={RoomId} MessageId={MessageId}", message.RoomId, message.Id);
            MessageReceived?.Invoke(this, message);
        });

        _hubConnection.Connection.On<int, string>("MessageEdited", (messageId, newContent) =>
        {
            _logger.LogDebug("MessageEdited event: MessageId={MessageId}", messageId);
            MessageEdited?.Invoke(this, (messageId, newContent));
        });

        _hubConnection.Connection.On<int>("MessageDeleted", messageId =>
        {
            _logger.LogDebug("MessageDeleted event: MessageId={MessageId}", messageId);
            MessageDeleted?.Invoke(this, messageId);
        });

        _hubConnection.Connection.On<MessageAckDto>("MessageAck", ack =>
        {
            _logger.LogDebug("MessageAck received: ClientId={ClientId} ServerId={ServerId}", ack.ClientMessageId, ack.ServerMessageId);
            MessageAcknowledged?.Invoke(this, ack);
        });

        _hubConnection.Connection.On<RoomDto>("RoomUpdated", room =>
        {
            _logger.LogDebug("RoomUpdated event: RoomId={RoomId}", room.Id);
            RoomUpdated?.Invoke(this, room);
        });

        _hubConnection.Connection.On<int>("RoomJoined", roomId =>
        {
            _logger.LogDebug("RoomJoined event: RoomId={RoomId}", roomId);
            RoomJoined?.Invoke(this, roomId);
        });

        _hubConnection.Connection.On<int>("RoomLeft", roomId =>
        {
            _logger.LogDebug("RoomLeft event: RoomId={RoomId}", roomId);
            RoomLeft?.Invoke(this, roomId);
        });
    }

    public async Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int roomId, int? beforeMessageId = null)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetMessagesAsync called without an authenticated session");
            return new ApiResponse<List<MessageDto>> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Fetching messages for room: {RoomId}, beforeMessageId: {BeforeMessageId}", roomId, beforeMessageId);
        return await _hubConnection.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetMessages",
            token,
            roomId,
            50,
            beforeMessageId);
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(int roomId, string content)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("SendMessageAsync called without an authenticated session");
            return new ApiResponse<MessageDto> { Success = false, Message = "Not authenticated" };
        }

        var clientMessageId = Guid.NewGuid().ToString();

        _logger.LogDebug("Sending message to room: {RoomId} with clientId: {ClientId}", roomId, clientMessageId);

        int? replyToMessageId = null;
        return await _hubConnection.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage",
            token,
            roomId,
            content,
            replyToMessageId,
            clientMessageId);
    }

    public async Task<ApiResponse<bool>> EditMessageAsync(int messageId, string newContent)
    {
        _logger.LogDebug("Editing message: {MessageId}", messageId);

        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("EditMessageAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "EditMessage",
            token,
            messageId,
            newContent);
    }

    public async Task<ApiResponse<bool>> DeleteMessageAsync(int messageId)
    {
        _logger.LogDebug("Deleting message: {MessageId}", messageId);

        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("DeleteMessageAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "DeleteMessage",
            token,
            messageId);
    }

    public async Task<ApiResponse<bool>> JoinRoomAsync(int roomId)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("JoinRoomAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "JoinRoom",
            token,
            roomId);
    }
}
