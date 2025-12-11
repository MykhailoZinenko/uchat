using Newtonsoft.Json;
using uchat_common.Dtos;
using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;
using uchat_server.Repositories.Interfaces;

namespace uchat_server.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberRepository _roomMemberRepository;
    private readonly IMessageEditRepository _messageEditRepository;
    private readonly IMessageDeletionRepository _messageDeletionRepository;
    private readonly IMessageDeliveryStatusRepository _deliveryStatusRepository;
    private readonly IUserPtsRepository _userPtsRepository;
    private readonly IMessageQueueRepository _messageQueueRepository;
    private readonly IUserUpdateRepository _userUpdateRepository;

    public MessageService(
        IMessageRepository messageRepository,
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository,
        IMessageEditRepository messageEditRepository,
        IMessageDeletionRepository messageDeletionRepository,
        IMessageDeliveryStatusRepository deliveryStatusRepository,
        IUserPtsRepository userPtsRepository,
        IMessageQueueRepository messageQueueRepository,
        IUserUpdateRepository userUpdateRepository)
    {
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
        _messageEditRepository = messageEditRepository;
        _messageDeletionRepository = messageDeletionRepository;
        _deliveryStatusRepository = deliveryStatusRepository;
        _userPtsRepository = userPtsRepository;
        _messageQueueRepository = messageQueueRepository;
        _userUpdateRepository = userUpdateRepository;
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

        // Create message with initial Pending status
        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = senderUserId,
            MessageType = MessageType.Text,
            ServiceAction = null,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            SenderDeliveryStatus = DeliveryStatus.Pending
        };

        var createdMessage = await _messageRepository.CreateAsync(message);

        // Transition to Sent status (server has received and saved the message)
        createdMessage.SenderDeliveryStatus = DeliveryStatus.Sent;
        createdMessage.SenderAcknowledgedAt = DateTime.UtcNow;
        await _messageRepository.UpdateAsync(createdMessage);

        // Create delivery status records for all room members (except sender)
        var roomMembers = await _roomMemberRepository.GetMembersByRoomIdAsync(roomId);
        foreach (var member in roomMembers.Where(m => m.UserId != senderUserId && m.LeftAt == null))
        {
            await _deliveryStatusRepository.CreateAsync(new MessageDeliveryStatus
            {
                MessageId = createdMessage.Id,
                UserId = member.UserId,
                Status = DeliveryStatus.Sent
            });

            // Create user update for each recipient
            var pts = await _userPtsRepository.IncrementPtsAsync(member.UserId);
            await CreateUserUpdateAsync(member.UserId, pts, "new_message", new
            {
                messageId = createdMessage.Id,
                roomId = roomId,
                senderId = senderUserId,
                content = content,
                sentAt = createdMessage.SentAt
            });
        }

        return createdMessage;
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

        // Fetch delivery statuses for the current user for all messages in one query
        var deliveryStatuses = await _deliveryStatusRepository.GetByMessageIdsAndUserIdAsync(messageIds, userId);
        var deliveryStatusMap = deliveryStatuses.ToDictionary(ds => ds.MessageId, ds => ds);

        var result = new List<MessageDto>();
        foreach (var m in messages)
        {
            if (deletedIds.Contains(m.Id))
            {
                continue;
            }

            var latestEdit = await _messageEditRepository.GetLatestEditAsync(m.Id);

            // Determine delivery status based on whether user is sender or recipient
            DeliveryStatus status;
            DateTime? deliveredAt = null;
            DateTime? readAt = null;

            if (m.SenderUserId == userId)
            {
                // User is the sender - use sender's delivery status
                status = m.SenderDeliveryStatus;
            }
            else
            {
                // User is a recipient - look up their specific delivery status
                if (deliveryStatusMap.TryGetValue(m.Id, out var deliveryStatus))
                {
                    status = deliveryStatus.Status;
                    deliveredAt = deliveryStatus.DeliveredAt;
                    readAt = deliveryStatus.ReadAt;
                }
                else
                {
                    // No delivery status record found - default to Sent
                    status = DeliveryStatus.Sent;
                }
            }

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
                IsDeleted = false,
                DeliveryStatus = status,
                DeliveredAt = deliveredAt,
                ReadAt = readAt
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

    public async Task<int> GetNextPtsAsync(int userId)
    {
        var current = await _userPtsRepository.GetCurrentPtsAsync(userId);
        return current + 1;
    }

    public async Task<int> IncrementPtsAsync(int userId)
    {
        return await _userPtsRepository.IncrementPtsAsync(userId);
    }

    public async Task<int> GetUnreadCountAsync(int roomId, int userId)
    {
        return await _deliveryStatusRepository.GetUnreadCountAsync(userId, roomId);
    }

    public async Task AddUserUpdateAsync(int userId, UserUpdateDto update)
    {
        await CreateUserUpdateAsync(userId, update.Pts, "user_update", update);
    }

    public async Task<List<UserUpdateDto>> GetUserUpdatesAsync(int userId, int fromPts, int limit = 100)
    {
        var updates = await _userUpdateRepository.GetUpdatesAsync(userId, fromPts, limit);
        var result = new List<UserUpdateDto>();

        foreach (var update in updates)
        {
            try
            {
                var dto = JsonConvert.DeserializeObject<UserUpdateDto>(update.UpdateData);
                if (dto != null)
                {
                    result.Add(dto);
                }
            }
            catch
            {
                // Skip invalid updates
            }
        }

        return result;
    }

    public async Task MarkMessageAsDeliveredAsync(int messageId, int userId)
    {
        await _deliveryStatusRepository.UpdateStatusAsync(messageId, userId, DeliveryStatus.Delivered);
    }

    public async Task MarkMessageAsReadAsync(int messageId, int userId)
    {
        await _deliveryStatusRepository.UpdateStatusAsync(messageId, userId, DeliveryStatus.Read);
    }

    public async Task<List<MessageQueue>> GetPendingMessagesAsync(int userId)
    {
        return await _messageQueueRepository.GetPendingMessagesAsync(userId);
    }

    public async Task QueueMessageForOfflineUserAsync(int messageId, int recipientUserId)
    {
        await _messageQueueRepository.QueueMessageAsync(messageId, recipientUserId);
    }

    private async Task CreateUserUpdateAsync(int userId, int pts, string updateType, object updateData)
    {
        var jsonData = JsonConvert.SerializeObject(updateData);
        await _userUpdateRepository.CreateUpdateAsync(userId, pts, updateType, jsonData);
    }
}
