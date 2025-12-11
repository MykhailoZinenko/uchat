namespace uchat_server.Data.Entities;

public class MessageDeletion
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int DeletedByUserId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
    public User DeletedBy { get; set; } = null!;
}
