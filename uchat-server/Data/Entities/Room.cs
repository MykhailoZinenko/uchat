using uchat_common.Enums;

namespace uchat_server.Data.Entities;

public class Room
{
    public int Id { get; set; }
    public RoomType RoomType { get; set; }
    public string? RoomName { get; set; }
    public string? RoomDescription { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsGlobal { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User? CreatedBy { get; set; }
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
