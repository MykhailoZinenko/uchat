using uchat_common.Enums;

namespace uchat_common.Dtos;

public class RoomMemberDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public MemberRole MemberRole { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsMuted { get; set; }
}
