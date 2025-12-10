using uchat_common.Dtos;
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
    private readonly IMessageEditRepository _messageEditRepository;
    private readonly IMessageDeletionRepository _messageDeletionRepository;
    private readonly IBlockedUserRepository _blockedUserRepository;

    public MessageService(
        IMessageRepository messageRepository,
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository,
        IMessageEditRepository messageEditRepository,
        IMessageDeletionRepository messageDeletionRepository,
        IBlockedUserRepository blockedUserRepository)
    {
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
        _messageEditRepository = messageEditRepository;
        _messageDeletionRepository = messageDeletionRepository;
        _blockedUserRepository = blockedUserRepository;
    }

    public async Task<Message> SendMessageAsync(int roomId, int senderUserId, string content, int? replyToMessageId = null)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (!room.IsGlobal)
        {
            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, senderUserId);
            if (member == null || member.LeftAt != null)
            {
                throw new ForbiddenException("You are not a member of this room");
            }

            var members = await _roomMemberRepository.GetMembersByRoomIdAsync(roomId);
            foreach (var otherMember in members.Where(m => m.UserId != senderUserId && m.LeftAt == null))
            {
                var isBlocked = await _blockedUserRepository.IsBlockedAsync(senderUserId, otherMember.UserId);
                if (isBlocked)
                {
                    throw new ForbiddenException("Cannot send message - you have blocked or been blocked by a member of this room");
                }
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

    public async Task<List<MessageDto>> GetMessagesAsync(int roomId, int userId, int limit = 50, int? beforeMessageId = null)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (!room.IsGlobal)
        {
            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
            if (member == null || member.LeftAt != null)
            {
                throw new ForbiddenException("You are not a member of this room");
            }
        }

        var messages = await _messageRepository.GetByRoomIdAsync(roomId, limit, beforeMessageId);
        
        var messageIds = messages.Select(m => m.Id).ToList();
        var deletedIds = await _messageDeletionRepository.GetDeletedMessageIdsAsync(messageIds);
        
        var result = new List<MessageDto>();
        foreach (var m in messages)
        {
            if (deletedIds.Contains(m.Id))
            {
                continue;
            }

            var latestEdit = await _messageEditRepository.GetLatestEditAsync(m.Id);
            
            result.Add(new MessageDto
            {
                Id = m.Id,
                RoomId = m.RoomId,
                SenderUserId = m.SenderUserId,
                SenderUsername = m.Sender?.Username,
                MessageType = m.MessageType,
                ServiceAction = m.ServiceAction,
                ReplyToMessageId = m.ReplyToMessageId,
                Content = latestEdit?.NewContent ?? m.Content,
                SentAt = m.SentAt,
                IsEdited = latestEdit != null,
                EditedAt = latestEdit?.EditedAt,
                IsDeleted = false
            });
        }

        return result;
    }

    public async Task<Message> EditMessageAsync(int messageId, int userId, string newContent)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            throw new NotFoundException("Message not found");
        }

        if (message.SenderUserId != userId)
        {
            throw new ForbiddenException("You can only edit your own messages");
        }

        var deletion = await _messageDeletionRepository.GetByMessageIdAsync(messageId);
        if (deletion != null)
        {
            throw new ValidationException("Cannot edit deleted message");
        }

        if (string.IsNullOrWhiteSpace(newContent))
        {
            throw new ValidationException("Message content cannot be empty");
        }

        var latestEdit = await _messageEditRepository.GetLatestEditAsync(messageId);
        var oldContent = latestEdit?.NewContent ?? message.Content;

        var edit = new MessageEdit
        {
            MessageId = messageId,
            EditedByUserId = userId,
            OldContent = oldContent,
            NewContent = newContent
        };

        await _messageEditRepository.CreateAsync(edit);

        return message;
    }

    public async Task DeleteMessageAsync(int messageId, int userId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null)
        {
            throw new NotFoundException("Message not found");
        }

        if (message.SenderUserId != userId)
        {
            throw new ForbiddenException("You can only delete your own messages");
        }

        var existingDeletion = await _messageDeletionRepository.GetByMessageIdAsync(messageId);
        if (existingDeletion != null)
        {
            throw new ValidationException("Message is already deleted");
        }

        var deletion = new MessageDeletion
        {
            MessageId = messageId,
            DeletedByUserId = userId
        };

        await _messageDeletionRepository.CreateAsync(deletion);
    }
}
