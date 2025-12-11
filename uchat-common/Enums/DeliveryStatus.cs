namespace uchat_common.Enums;

/// <summary>
/// Message delivery status following Telegram-like progression:
/// Pending -> Sent -> Delivered -> Read
/// </summary>
public enum DeliveryStatus
{
    /// <summary>
    /// Message created but not yet confirmed sent to server
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message received and saved by server (single checkmark)
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message delivered to recipient's device (double checkmark)
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message seen/read by recipient (blue double checkmark)
    /// </summary>
    Read = 3
}
