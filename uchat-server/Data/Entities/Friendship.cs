namespace uchat_server.Data.Entities;

public class Friendship
{
    public int FriendshipId { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public string Status { get; set; } = "pending"; // 'pending', 'accepted', 'rejected'
    public int InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public User InitiatedBy { get; set; } = null!;
}
