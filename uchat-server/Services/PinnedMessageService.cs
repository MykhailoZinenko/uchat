using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class PinnedMessageService : IPinnedMessageService
{
    private readonly IPinnedMessageRepository _pinnedMessageRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberRepository _roomMemberRepository;

    public PinnedMessageService(
        IPinnedMessageRepository pinnedMessageRepository,
        IMessageRepository messageRepository,
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository)
    {
        _pinnedMessageRepository = pinnedMessageRepository;
        _messageRepository = messageRepository;
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
    }

    public async Task<PinnedMessage> PinMessageAsync(int roomId, int messageId, int userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null || message.RoomId != roomId)
        {
            throw new NotFoundException("Message not found in this room");
        }

        if (!room.IsGlobal)
        {
            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
            if (member == null || member.LeftAt != null)
            {
                throw new ForbiddenException("You are not a member of this room");
            }

            if (member.MemberRole != MemberRole.Owner && member.MemberRole != MemberRole.Admin)
            {
                throw new ForbiddenException("Only owner or admin can pin messages");
            }
        }

        var existing = await _pinnedMessageRepository.GetByRoomAndMessageAsync(roomId, messageId);
        if (existing != null)
        {
            throw new ValidationException("Message is already pinned");
        }

        var pinnedMessage = new PinnedMessage
        {
            RoomId = roomId,
            MessageId = messageId,
            PinnedByUserId = userId
        };

        return await _pinnedMessageRepository.CreateAsync(pinnedMessage);
    }

    public async Task UnpinMessageAsync(int roomId, int messageId, int userId)
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

            if (member.MemberRole != MemberRole.Owner && member.MemberRole != MemberRole.Admin)
            {
                throw new ForbiddenException("Only owner or admin can unpin messages");
            }
        }

        var pinnedMessage = await _pinnedMessageRepository.GetByRoomAndMessageAsync(roomId, messageId);
        if (pinnedMessage == null)
        {
            throw new NotFoundException("Message is not pinned");
        }

        await _pinnedMessageRepository.DeleteAsync(pinnedMessage);
    }

    public async Task<List<PinnedMessage>> GetPinnedMessagesAsync(int roomId, int userId)
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

        return await _pinnedMessageRepository.GetByRoomIdAsync(roomId);
    }
}
