using uchat_common.Enums;

namespace uchat_server.Data.Entities;

public class RoomMember
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public MemberRole MemberRole { get; set; } = MemberRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public int? LastReadMessageId { get; set; }
    public bool IsMuted { get; set; }

    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
    public Message? LastReadMessage { get; set; }
}
