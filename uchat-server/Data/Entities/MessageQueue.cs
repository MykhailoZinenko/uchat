namespace uchat_server.Data.Entities;

/// <summary>
/// Queue for messages to be delivered to offline users.
/// When a user comes online, messages from this queue are sent to them.
/// </summary>
public class MessageQueue
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int RecipientUserId { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public bool IsDelivered { get; set; } = false;
    public DateTime? DeliveredAt { get; set; }

    public Message Message { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}
