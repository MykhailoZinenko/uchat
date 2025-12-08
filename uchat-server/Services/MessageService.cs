using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberRepository _roomMemberRepository;

    public MessageService(
        IMessageRepository messageRepository,
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository)
    {
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
    }

    public async Task<Message> SendMessageAsync(int roomId, int senderUserId, string content, int? replyToMessageId = null)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        // Check if user can send messages
        if (!room.IsGlobal)
        {
            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, senderUserId);
            if (member == null || member.LeftAt != null)
            {
                throw new ForbiddenException("You are not a member of this room");
            }
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ValidationException("Message content cannot be empty");
        }

        if (replyToMessageId.HasValue)
        {
            var replyToMessage = await _messageRepository.GetByIdAsync(replyToMessageId.Value);
            if (replyToMessage == null || replyToMessage.RoomId != roomId)
            {
                throw new ValidationException("Reply message not found in this room");
            }
        }

        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = senderUserId,
            MessageType = MessageType.Text,
            ServiceAction = null,
            Content = content,
            ReplyToMessageId = replyToMessageId
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<Message> SendSystemMessageAsync(int roomId, ServiceAction action, string content, int? relatedUserId = null)
    {
        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = relatedUserId,
            MessageType = MessageType.Service,
            ServiceAction = action,
            Content = content
        };

        return await _messageRepository.CreateAsync(message);
    }

    public async Task<List<Message>> GetMessagesAsync(int roomId, int userId, int limit = 50, int? beforeMessageId = null)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        // Check if user can view messages
        if (!room.IsGlobal)
        {
            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
            if (member == null || member.LeftAt != null)
            {
                throw new ForbiddenException("You are not a member of this room");
            }
        }

        return await _messageRepository.GetByRoomIdAsync(roomId, limit, beforeMessageId);
    }
}
