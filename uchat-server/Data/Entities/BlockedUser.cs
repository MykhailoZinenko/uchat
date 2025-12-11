namespace uchat_server.Data.Entities;

public class BlockedUser
{
    public int Id { get; set; }
    public int BlockerUserId { get; set; }
    public int BlockedUserId { get; set; }
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

    public User Blocker { get; set; } = null!;
    public User Blocked { get; set; } = null!;
}

