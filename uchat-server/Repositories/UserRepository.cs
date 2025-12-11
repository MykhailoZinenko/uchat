using Microsoft.EntityFrameworkCore;
using uchat_server.Data;
using uchat_server.Data.Entities;

namespace uchat_server.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UchatDbContext _context;

    public UserRepository(UchatDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetByUsernameAndPasswordAsync(string username, string passwordHash)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.PasswordHash == passwordHash);
    }

    public async Task<List<User>> SearchByUsernameAsync(string query, int limit = 20)
    {
        var lowerQuery = query.ToLower();
        return await _context.Users
            .Where(u => u.Username.ToLower().Contains(lowerQuery))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
