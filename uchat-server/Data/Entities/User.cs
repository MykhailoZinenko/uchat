namespace uchat_server.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? StatusText { get; set; }
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<RoomMember> RoomMemberships { get; set; } = new List<RoomMember>();
    public ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
}
