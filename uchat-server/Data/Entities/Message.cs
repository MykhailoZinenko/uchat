using uchat_common.Enums;

namespace uchat_server.Data.Entities;

public class Message
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int? SenderUserId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public ServiceAction? ServiceAction { get; set; }
    public int? ReplyToMessageId { get; set; }
    public int? ForwardedFromMessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
    public User? Sender { get; set; }
    public Message? ReplyToMessage { get; set; }
    public Message? ForwardedFromMessage { get; set; }
    public ICollection<MessageEdit> Edits { get; set; } = new List<MessageEdit>();
    public MessageDeletion? Deletion { get; set; }
}


