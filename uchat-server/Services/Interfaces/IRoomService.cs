using uchat_common.Enums;
using uchat_server.Data.Entities;

namespace uchat_server.Services;

public interface IRoomService
{
    Task<List<Room>> GetAccessibleRoomsAsync(int userId);
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<Room> CreateRoomAsync(int creatorUserId, RoomType type, string? name, string? description);
    Task<Room> UpdateRoomAsync(int roomId, int requestingUserId, string? name, string? description, string? avatarUrl);
    Task DeleteRoomAsync(int roomId, int requestingUserId);
}

