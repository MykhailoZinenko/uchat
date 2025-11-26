namespace uchat_server.Data.Entities;

public class Session
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;

    public User User { get; set; } = null!;
}
