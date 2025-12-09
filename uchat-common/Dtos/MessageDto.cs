using uchat_common.Enums;

namespace uchat_common.Dtos;

public class MessageDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int? SenderUserId { get; set; }
    public string? SenderUsername { get; set; }
    public MessageType MessageType { get; set; }
    public ServiceAction? ServiceAction { get; set; }
    public int? ReplyToMessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
}


