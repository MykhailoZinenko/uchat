using uchat_common.Enums;

namespace uchat_common.Dtos;

public class RoomDto
{
    public int Id { get; set; }
    public RoomType RoomType { get; set; }
    public string? RoomName { get; set; }
    public string? RoomDescription { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsGlobal { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
