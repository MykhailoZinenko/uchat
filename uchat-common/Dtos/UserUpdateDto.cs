using System;
using uchat_common.Enums;

namespace uchat_common.Dtos;

public class UserUpdateDto
{
    public int Pts { get; set; }
    public UserUpdateType Type { get; set; }
    public DialogUpdateDto? Dialog { get; set; }
}

public class DialogUpdateDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public int MessageId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public int UnreadCount { get; set; }
}
