using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IMessageService
{
    Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int roomId);
    Task<ApiResponse<MessageDto>> SendMessageAsync(int roomId, string content);
    Task<ApiResponse<MessageDto>> EditMessageAsync(int messageId, string newContent);
    Task<ApiResponse<bool>> DeleteMessageAsync(int messageId);
    Task<ApiResponse<bool>> JoinRoomAsync(int roomId);
    Task<ApiResponse<List<UserUpdateDto>>> GetUserUpdatesAsync(int fromPts);

    // Telegram-like delivery tracking methods
    Task<ApiResponse<bool>> MarkMessageDeliveredAsync(int messageId);
    Task<ApiResponse<bool>> MarkMessageReadAsync(int messageId);
    Task<ApiResponse<List<MessageDto>>> GetPendingMessagesAsync();

    event EventHandler<MessageDto>? MessageReceived;
    event EventHandler<MessageDto>? MessageEdited;
    event EventHandler<int>? MessageDeleted;
    event EventHandler<UserUpdateDto>? UserUpdateReceived;

    // New Telegram-like events
    event EventHandler<MessageAckDto>? MessageAcknowledged;
    event EventHandler<DeliveryReceiptDto>? DeliveryReceiptReceived;
}
