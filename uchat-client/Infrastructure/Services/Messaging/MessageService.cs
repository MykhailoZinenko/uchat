using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_common.Dtos;
using uchat_common.Enums;

namespace uchat_client.Infrastructure.Services.Messaging;

public class MessageService : IMessageService
{
    private readonly IHubConnectionService _hubConnection;
    private readonly ILoggingService _logger;

    public event EventHandler<MessageDto>? MessageReceived;
    public event EventHandler<MessageDto>? MessageEdited;
    public event EventHandler<int>? MessageDeleted;
    public event EventHandler<UserUpdateDto>? UserUpdateReceived;

    // New Telegram-like events
    public event EventHandler<MessageAckDto>? MessageAcknowledged;
    public event EventHandler<DeliveryReceiptDto>? DeliveryReceiptReceived;

    private readonly IAuthService _authService;
    private readonly Dictionary<string, int> _pendingMessages = new();

    public MessageService(
        IHubConnectionService hubConnection,
        IAuthService authService,
        ILoggingService logger)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Existing events
        _hubConnection.Connection.On<MessageDto>("MessageReceived", message =>
        {
            _logger.LogDebug("MessageReceived event: RoomId={RoomId} MessageId={MessageId}", message.RoomId, message.Id);
            MessageReceived?.Invoke(this, message);

            // Automatically mark as delivered when received
            _ = MarkMessageDeliveredAsync(message.Id);
        });

        _hubConnection.Connection.On<UserUpdateDto>("UserUpdate", update =>
        {
            UserUpdateReceived?.Invoke(this, update);
        });

        // New Telegram-like events
        _hubConnection.Connection.On<MessageAckDto>("MessageAck", ack =>
        {
            _logger.LogDebug("MessageAck received: ClientId={ClientId} ServerId={ServerId}", ack.ClientMessageId, ack.ServerMessageId);
            MessageAcknowledged?.Invoke(this, ack);
        });

        _hubConnection.Connection.On<DeliveryReceiptDto>("DeliveryReceipt", receipt =>
        {
            _logger.LogDebug("DeliveryReceipt: MessageId={MessageId} Status={Status}", receipt.MessageId, receipt.Status);
            DeliveryReceiptReceived?.Invoke(this, receipt);
        });
    }

    public async Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int roomId)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetMessagesAsync called without an authenticated session");
            return new ApiResponse<List<MessageDto>> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Fetching messages for room: {RoomId}", roomId);
        int? beforeMessageId = null;
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

        // Generate client message ID for tracking (Telegram-style)
        var clientMessageId = Guid.NewGuid().ToString();

        _logger.LogDebug("Sending message to room: {RoomId} with clientId: {ClientId}", roomId, clientMessageId);

        // Pass parameters explicitly for SignalR
        int? replyToMessageId = null;
        return await _hubConnection.InvokeAsync<ApiResponse<MessageDto>>(
            "SendMessage",
            token,
            roomId,
            content,
            replyToMessageId,
            clientMessageId);
    }

    public async Task<ApiResponse<MessageDto>> EditMessageAsync(int messageId, string newContent)
    {
        _logger.LogDebug("Editing message: {MessageId}", messageId);

        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("EditMessageAsync called without an authenticated session");
            return new ApiResponse<MessageDto> { Success = false, Message = "Not authenticated" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<MessageDto>>(
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

    public async Task<ApiResponse<List<UserUpdateDto>>> GetUserUpdatesAsync(int fromPts)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetUserUpdatesAsync called without an authenticated session");
            return new ApiResponse<List<UserUpdateDto>> { Success = false, Message = "Not authenticated" };
        }

        return await _hubConnection.InvokeAsync<ApiResponse<List<UserUpdateDto>>>(
            "GetUserUpdates",
            token,
            fromPts,
            100);
    }

    // ==================== TELEGRAM-LIKE DELIVERY TRACKING ====================

    public async Task<ApiResponse<bool>> MarkMessageDeliveredAsync(int messageId)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("MarkMessageDeliveredAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Marking message as delivered: {MessageId}", messageId);
        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "MarkMessageDelivered",
            token,
            messageId);
    }

    public async Task<ApiResponse<bool>> MarkMessageReadAsync(int messageId)
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("MarkMessageReadAsync called without an authenticated session");
            return new ApiResponse<bool> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Marking message as read: {MessageId}", messageId);
        return await _hubConnection.InvokeAsync<ApiResponse<bool>>(
            "MarkMessageRead",
            token,
            messageId);
    }

    public async Task<ApiResponse<List<MessageDto>>> GetPendingMessagesAsync()
    {
        var token = _authService.SessionToken;
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("GetPendingMessagesAsync called without an authenticated session");
            return new ApiResponse<List<MessageDto>> { Success = false, Message = "Not authenticated" };
        }

        _logger.LogDebug("Fetching pending messages");
        return await _hubConnection.InvokeAsync<ApiResponse<List<MessageDto>>>(
            "GetPendingMessages",
            token);
    }
}
