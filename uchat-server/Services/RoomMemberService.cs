using uchat_common.Enums;
using uchat_server.Data.Entities;
using uchat_server.Exceptions;
using uchat_server.Repositories;

namespace uchat_server.Services;

public class RoomMemberService : IRoomMemberService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomMemberRepository _roomMemberRepository;
    private readonly IMessageService _messageService;

    public RoomMemberService(
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository,
        IMessageService messageService)
    {
        _roomRepository = roomRepository;
        _roomMemberRepository = roomMemberRepository;
        _messageService = messageService;
    }

    public async Task JoinRoomAsync(int roomId, int userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        var existingMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);

        if (existingMember != null)
        {
            if (existingMember.LeftAt != null)
            {
                existingMember.LeftAt = null;
                existingMember.JoinedAt = DateTime.UtcNow;
                await _roomMemberRepository.UpdateAsync(existingMember);

                await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserJoined,
                    "User joined the room", userId);

                return;
            }
            return;
        }

        var newMember = new RoomMember
        {
            RoomId = roomId,
            UserId = userId,
            MemberRole = MemberRole.Member
        };

        await _roomMemberRepository.CreateAsync(newMember);

        await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserJoined,
            "User joined the room", userId);
    }

    public async Task LeaveRoomAsync(int roomId, int userId)
    {
        var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
        if (member == null || member.LeftAt != null)
        {
            throw new NotFoundException("You are not a member of this room");
        }

        if (member.MemberRole == MemberRole.Owner)
        {
            throw new ForbiddenException("Owner cannot leave the room. Transfer ownership first or delete the room.");
        }

        member.LeftAt = DateTime.UtcNow;
        await _roomMemberRepository.UpdateAsync(member);

        await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserLeft,
            "User left the room", userId);
    }

    public async Task<bool> CanUserAccessRoomAsync(int roomId, int userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        if (room.IsGlobal) return true;

        var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
        return member != null && member.LeftAt == null;
    }

    public async Task<RoomMember?> GetMemberAsync(int roomId, int userId)
    {
        return await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
    }

    public async Task<List<int>> GetMemberUserIdsAsync(int roomId)
    {
        var members = await _roomMemberRepository.GetMembersByRoomIdAsync(roomId);
        return members.Where(m => m.LeftAt == null).Select(m => m.UserId).ToList();
    }

    public async Task AddMembersAsync(int roomId, int requestingUserId, List<int> userIds)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (room.IsGlobal)
        {
            throw new ForbiddenException("Cannot add members to global room");
        }

        var requestingMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, requestingUserId);
        if (requestingMember == null || requestingMember.LeftAt != null)
        {
            throw new ForbiddenException("You are not a member of this room");
        }

        if (requestingMember.MemberRole != MemberRole.Owner && requestingMember.MemberRole != MemberRole.Admin)
        {
            throw new ForbiddenException("Only owner or admin can add members");
        }

        var newMembers = new List<RoomMember>();
        foreach (var userId in userIds)
        {
            var existingMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
            if (existingMember != null)
            {
                if (existingMember.LeftAt != null)
                {
                    // Rejoin the room
                    existingMember.LeftAt = null;
                    existingMember.JoinedAt = DateTime.UtcNow;
                    existingMember.MemberRole = MemberRole.Member;
                    await _roomMemberRepository.UpdateAsync(existingMember);

                    await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserJoined,
                        $"User joined the room", userId);
                }
                continue;
            }

            newMembers.Add(new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                MemberRole = MemberRole.Member
            });
        }

        if (newMembers.Count > 0)
        {
            await _roomMemberRepository.CreateRangeAsync(newMembers);

            foreach (var member in newMembers)
            {
                await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserJoined,
                    $"User joined the room", member.UserId);
            }
        }
    }

    public async Task RemoveMembersAsync(int roomId, int requestingUserId, List<int> userIds)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (room.IsGlobal)
        {
            throw new ForbiddenException("Cannot remove members from global room");
        }

        var requestingMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, requestingUserId);
        if (requestingMember == null || requestingMember.LeftAt != null)
        {
            throw new ForbiddenException("You are not a member of this room");
        }

        if (requestingMember.MemberRole != MemberRole.Owner && requestingMember.MemberRole != MemberRole.Admin)
        {
            throw new ForbiddenException("Only owner or admin can remove members");
        }

        foreach (var userId in userIds)
        {
            if (userId == requestingUserId && requestingMember.MemberRole == MemberRole.Owner)
            {
                throw new ForbiddenException("Owner cannot remove themselves. Transfer ownership first or delete the room.");
            }

            var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
            if (member != null && member.LeftAt == null)
            {
                member.LeftAt = DateTime.UtcNow;
                await _roomMemberRepository.UpdateAsync(member);

                await _messageService.SendSystemMessageAsync(roomId, ServiceAction.UserLeft,
                    $"User left the room", userId);
            }
        }
    }

    public async Task UpdateMemberRoleAsync(int roomId, int requestingUserId, int targetUserId, MemberRole newRole)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        if (room.IsGlobal)
        {
            throw new ForbiddenException("Cannot change roles in global room");
        }

        var requestingMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, requestingUserId);
        if (requestingMember == null || requestingMember.LeftAt != null)
        {
            throw new ForbiddenException("You are not a member of this room");
        }

        if (requestingMember.MemberRole != MemberRole.Owner)
        {
            throw new ForbiddenException("Only owner can change member roles");
        }

        var targetMember = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, targetUserId);
        if (targetMember == null || targetMember.LeftAt != null)
        {
            throw new NotFoundException("Target user is not a member of this room");
        }

        if (newRole == MemberRole.Owner)
        {
            // Transfer ownership
            requestingMember.MemberRole = MemberRole.Admin;
            targetMember.MemberRole = MemberRole.Owner;
            await _roomMemberRepository.UpdateAsync(requestingMember);
            await _roomMemberRepository.UpdateAsync(targetMember);
        }
        else
        {
            targetMember.MemberRole = newRole;
            await _roomMemberRepository.UpdateAsync(targetMember);
        }

        await _messageService.SendSystemMessageAsync(roomId, ServiceAction.MemberRoleChanged,
            $"Member role changed to {newRole}", targetUserId);
    }

    public async Task UpdateMutedStatusAsync(int roomId, int userId, bool isMuted)
    {
        var member = await _roomMemberRepository.GetByRoomAndUserAsync(roomId, userId);
        if (member == null || member.LeftAt != null)
        {
            throw new NotFoundException("You are not a member of this room");
        }

        member.IsMuted = isMuted;
        await _roomMemberRepository.UpdateAsync(member);
    }
}
