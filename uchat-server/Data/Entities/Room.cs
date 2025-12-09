namespace uchat_server.Data.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomType { get; set; } = string.Empty; // 'global', 'direct', 'group'
    public string? RoomName { get; set; }
    public string? RoomDescription { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsGlobal { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? CreatedBy { get; set; }
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<RoomPin> PinnedMessages { get; set; } = new List<RoomPin>();
}
