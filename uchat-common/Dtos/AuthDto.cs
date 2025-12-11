namespace uchat_common.Dtos;

public class AuthDto
{
    public string SessionToken { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

