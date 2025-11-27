namespace uchat_common.Dtos;

public class LoginResult
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public List<MessageDto> MessageHistory { get; set; } = new();
}
