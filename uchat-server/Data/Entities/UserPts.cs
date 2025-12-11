namespace uchat_server.Data.Entities;

/// <summary>
/// Persistent storage for user's Pts (Presence Timestamp Sequence).
/// Telegram-like update sequence tracking - clients use this to fetch missed updates.
/// </summary>
public class UserPts
{
    public int UserId { get; set; }
    public int CurrentPts { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
