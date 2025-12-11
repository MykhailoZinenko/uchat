namespace uchat_server.Data.Entities;

/// <summary>
/// Persistent storage for user updates (similar to Telegram's updates system).
/// Clients fetch updates by their last known Pts to stay in sync.
/// </summary>
public class UserUpdate
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Pts { get; set; }
    public string UpdateType { get; set; } = string.Empty;
    public string UpdateData { get; set; } = string.Empty; // JSON serialized update
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
