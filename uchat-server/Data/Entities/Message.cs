namespace uchat_server.Data.Entities;

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }

    public User Sender { get; set; } = null!;
}
