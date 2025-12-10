namespace uchat_server.Data.Entities;

public class MessageEdit
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int EditedByUserId { get; set; }
    public string OldContent { get; set; } = string.Empty;
    public string NewContent { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
    public User EditedBy { get; set; } = null!;
}
