namespace uchat_server.Data.Entities;

public class PinnedMessage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int MessageId { get; set; }
    public int PinnedByUserId { get; set; }
    public DateTime PinnedAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
    public Message Message { get; set; } = null!;
    public User PinnedBy { get; set; } = null!;
}
