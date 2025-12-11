using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IRoomService
{
    Task<ApiResponse<List<RoomDto>>> GetAccessibleRoomsAsync();
    Task<ApiResponse<RoomDto>> CreateRoomAsync(string type, string? name, string? description);
    Task<ApiResponse<bool>> JoinRoomAsync(int roomId);
    Task<ApiResponse<bool>> LeaveRoomAsync(int roomId);
    Task<ApiResponse<bool>> AddRoomMembersAsync(int roomId, List<int> userIds);
}
