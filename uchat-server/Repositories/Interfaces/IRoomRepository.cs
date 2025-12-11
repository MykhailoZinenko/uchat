using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);
    Task<List<Room>> GetByIdsAsync(List<int> ids);
    Task<int?> GetGlobalRoomIdAsync();
    Task<Room> CreateAsync(Room room);
    Task<Room> UpdateAsync(Room room);
    Task DeleteAsync(Room room);
}
