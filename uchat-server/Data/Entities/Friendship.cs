using uchat_common.Enums;

namespace uchat_server.Data.Entities;

public class Friendship
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public int InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public User InitiatedBy { get; set; } = null!;
}

