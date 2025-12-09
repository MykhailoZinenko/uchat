namespace uchat_server.Data.Entities;

public class MessageDeletion
{
    public int DeletionId { get; set; }
    public int MessageId { get; set; }
    public int DeletedByUserId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Message Message { get; set; } = null!;
    public User DeletedBy { get; set; } = null!;
}
