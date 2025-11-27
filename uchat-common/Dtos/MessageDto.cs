namespace uchat_common.Dtos;

public class MessageDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
