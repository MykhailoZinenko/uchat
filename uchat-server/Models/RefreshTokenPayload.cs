namespace uchat_server.Models;

public class RefreshTokenPayload
{
    public int UserId { get; set; }
    public int SessionId { get; set; }

    public RefreshTokenPayload(int userId, int sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }
}
