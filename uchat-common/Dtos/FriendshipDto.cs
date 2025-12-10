using uchat_common.Enums;

namespace uchat_common.Dtos;

public class FriendshipDto
{
    public int Id { get; set; }
    public int FriendUserId { get; set; }
    public string FriendUsername { get; set; } = string.Empty;
    public FriendshipStatus Status { get; set; }
    public bool IsInitiator { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
