namespace uchat_server.Data.Entities;

public class Session
{
    public int Id { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
