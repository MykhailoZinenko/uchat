using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberRepository _roomMemberRepository;

    public RoomService(IRoomRepository roomRepository, IRoomMemberRepository roomMemberRepository)
    {
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
    }

    public async Task<List<Room>> GetAccessibleRoomsAsync(int userId)
    {
        // Ensure a global room exists (seed may have failed or DB reset)
        var globalRoomId = await _roomRepository.GetGlobalRoomIdAsync();
        if (globalRoomId == null)
        {
            var globalRoom = new Room
            {
                RoomType = RoomType.Global,
                RoomName = "Global Chat",
                RoomDescription = "Welcome to uchat! This is the global chat room where everyone can communicate.",
                IsGlobal = true,
                CreatedByUserId = null
            };

            var created = await _roomRepository.CreateAsync(globalRoom);
            globalRoomId = created.Id;
        }

        var roomIds = await _roomMemberRepository.GetAccessibleRoomIdsAsync(userId);
        return await _roomRepository.GetByIdsAsync(roomIds);
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        return await _roomRepository.GetByIdAsync(roomId);
    }

    public async Task<Room> CreateRoomAsync(int creatorUserId, RoomType type, string? name, string? description)
    {
        if (type == RoomType.Global)
        {
            throw new ValidationException("Cannot create global rooms");
        }

        if (type == RoomType.Group && string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Group rooms require a name");
        }

        var room = new Room
        {
            RoomType = type,
            RoomName = name,
            RoomDescription = description,
            IsGlobal = false,
            CreatedByUserId = creatorUserId
        };

        await _roomRepository.CreateAsync(room);

        var ownerMember = new RoomMember
        {
            RoomId = room.Id,
            UserId = creatorUserId,
            MemberRole = MemberRole.Owner
        };

        await _roomMemberRepository.CreateAsync(ownerMember);

        return room;
    }

    public async Task<Room> UpdateRoomAsync(int roomId, int requestingUserId, string? name, string? description, string? avatarUrl)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (room.IsGlobal)
        {
            throw new ForbiddenException("Cannot modify global room");
        }

        var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, requestingUserId);
        if (member == null || member.LeftAt != null)
        {
            throw new ForbiddenException("You are not a member of this room");
        }

        if (member.MemberRole != MemberRole.Owner && member.MemberRole != MemberRole.Admin)
        {
            throw new ForbiddenException("Only owner or admin can update room");
        }

        if (name != null) room.RoomName = name;
        if (description != null) room.RoomDescription = description;
        if (avatarUrl != null) room.AvatarUrl = avatarUrl;

        await _roomRepository.UpdateAsync(room);
        return room;
    }

    public async Task DeleteRoomAsync(int roomId, int requestingUserId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (room.IsGlobal)
        {
            throw new ForbiddenException("Cannot delete global room");
        }

        var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, requestingUserId);
        if (member == null || member.LeftAt != null)
        {
            throw new ForbiddenException("You are not a member of this room");
        }

        if (member.MemberRole != MemberRole.Owner)
        {
            throw new ForbiddenException("Only owner can delete room");
        }

        await _roomRepository.DeleteAsync(room);
    }
}
