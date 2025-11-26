namespace uchat_common.Models;

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int? RecipientId { get; set; }
    public int? RoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}
