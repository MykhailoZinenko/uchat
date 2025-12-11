using uchat_common.Enums;

namespace uchat_common.Dtos;

/// <summary>
/// Delivery/read receipt sent between client and server to track message status.
/// </summary>
public class DeliveryReceiptDto
{
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
}
