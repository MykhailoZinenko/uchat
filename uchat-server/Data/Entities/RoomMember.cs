namespace uchat_server.Data.Entities;

public class RoomMember
{
    public int MemberId { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string MemberRole { get; set; } = "member"; // 'owner', 'admin', 'member'
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public int? LastReadMessageId { get; set; }
    public bool IsMuted { get; set; }

    // Navigation properties
    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
    public Message? LastReadMessage { get; set; }
}
