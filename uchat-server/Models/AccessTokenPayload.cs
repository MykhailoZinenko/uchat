namespace uchat_server.Models;

public class AccessTokenPayload
{
    public int UserId { get; set; }
    public int SessionId { get; set; }

    public AccessTokenPayload(int userId, int sessionId)
    {
        UserId = userId;
        SessionId = sessionId;
    }
}
