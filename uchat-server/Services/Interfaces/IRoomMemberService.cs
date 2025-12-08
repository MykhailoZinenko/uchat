using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IRoomMemberService
{
    Task JoinRoomAsync(int roomId, int userId);
    Task LeaveRoomAsync(int roomId, int userId);
    Task AddMembersAsync(int roomId, int requestingUserId, List<int> userIds);
    Task RemoveMembersAsync(int roomId, int requestingUserId, List<int> userIds);
    Task UpdateMemberRoleAsync(int roomId, int requestingUserId, int targetUserId, MemberRole newRole);
    Task UpdateMutedStatusAsync(int roomId, int userId, bool isMuted);
    Task<bool> CanUserAccessRoomAsync(int roomId, int userId);
    Task<RoomMember?> GetMemberAsync(int roomId, int userId);
}

