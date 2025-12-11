namespace uchat_common.Dtos;

/// <summary>
/// Acknowledgment sent from server to client when message is received and saved.
/// Client uses this to replace temporary message ID with real server ID.
/// </summary>
public class MessageAckDto
{
    public string? ClientMessageId { get; set; } // Temporary ID from client
    public int ServerMessageId { get; set; } // Real ID from database
    public DateTime SentAt { get; set; }
}
