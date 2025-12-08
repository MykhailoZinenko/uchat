using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class RoomMemberRepository : IRoomMemberRepository
{
    private readonly UchatDbContext _context;

    public RoomMemberRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<RoomMember?> GetByIdAsync(int id)
    {
        return await _context.RoomMembers.FindAsync(id);
    }

    public async Task<RoomMember?> GetByRoomAndUserAsync(int roomId, int userId)
    {
        return await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);
    }

    public async Task<List<int>> GetAccessibleRoomIdsAsync(int userId)
    {
        // Get rooms where user is a member (not left) + global rooms
        var memberRoomIds = await _context.RoomMembers
            .Where(rm => rm.UserId == userId && rm.LeftAt == null)
            .Select(rm => rm.RoomId)
            .ToListAsync();

        var globalRoomIds = await _context.Rooms
            .Where(r => r.IsGlobal)
            .Select(r => r.Id)
            .ToListAsync();

        return memberRoomIds.Union(globalRoomIds).Distinct().ToList();
    }

    public async Task<List<RoomMember>> GetMembersByRoomIdAsync(int roomId)
    {
        return await _context.RoomMembers
            .Where(rm => rm.RoomId == roomId && rm.LeftAt == null)
            .Include(rm => rm.User)
            .ToListAsync();
    }

    public async Task<RoomMember> CreateAsync(RoomMember member)
    {
        member.JoinedAt = DateTime.UtcNow;
        _context.RoomMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task CreateRangeAsync(List<RoomMember> members)
    {
        var now = DateTime.UtcNow;
        foreach (var member in members)
        {
            member.JoinedAt = now;
        }
        _context.RoomMembers.AddRange(members);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RoomMember member)
    {
        _context.RoomMembers.Update(member);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(List<RoomMember> members)
    {
        _context.RoomMembers.UpdateRange(members);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync(List<RoomMember> members)
    {
        _context.RoomMembers.RemoveRange(members);
        await _context.SaveChangesAsync();
    }
}
