using uchat_common.Enums;

namespace uchat_server.Data.Entities;

/// <summary>
/// Tracks delivery and read status for each message per recipient user.
/// This enables Telegram-like delivery tracking with sent/delivered/read states.
/// </summary>
public class MessageDeliveryStatus
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
