using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IMessageService
{
    Task<ApiResponse<List<MessageDto>>> GetMessagesAsync(int roomId, int? beforeMessageId = null);
    Task<ApiResponse<MessageDto>> SendMessageAsync(int roomId, string content);
    Task<ApiResponse<bool>> EditMessageAsync(int messageId, string newContent);
    Task<ApiResponse<bool>> DeleteMessageAsync(int messageId);
    Task<ApiResponse<bool>> JoinRoomAsync(int roomId);

    event EventHandler<MessageDto>? MessageReceived;
    event EventHandler<(int messageId, string newContent)>? MessageEdited;
    event EventHandler<int>? MessageDeleted;
    event EventHandler<MessageAckDto>? MessageAcknowledged;
}
