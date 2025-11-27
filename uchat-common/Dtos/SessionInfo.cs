namespace uchat_common.Dtos;

public class SessionInfo
{
    public string Token { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
