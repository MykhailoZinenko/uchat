using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly UchatDbContext _context;

    public RoomRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms.FindAsync(id);
    }

    public async Task<List<Room>> GetByIdsAsync(List<int> ids)
    {
        return await _context.Rooms
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();
    }

    public async Task<int?> GetGlobalRoomIdAsync()
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.IsGlobal);
        return room?.Id;
    }

    public async Task<Room> CreateAsync(Room room)
    {
        room.CreatedAt = DateTime.UtcNow;
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<Room> UpdateAsync(Room room)
    {
        room.UpdatedAt = DateTime.UtcNow;
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task DeleteAsync(Room room)
    {
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
    }
}
