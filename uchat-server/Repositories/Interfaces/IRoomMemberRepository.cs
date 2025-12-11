using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IRoomMemberRepository
{
    Task<RoomMember?> GetByIdAsync(int id);
    Task<RoomMember?> GetByRoomAndUserAsync(int roomId, int userId);
    Task<List<int>> GetAccessibleRoomIdsAsync(int userId);
    Task<List<RoomMember>> GetMembersByRoomIdAsync(int roomId);
    Task<RoomMember> CreateAsync(RoomMember member);
    Task CreateRangeAsync(List<RoomMember> members);
    Task UpdateAsync(RoomMember member);
    Task UpdateRangeAsync(List<RoomMember> members);
    Task DeleteRangeAsync(List<RoomMember> members);
}
