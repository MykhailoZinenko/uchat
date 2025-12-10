namespace uchat_common.Dtos;

public class AuthDto
{
    public int UserId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
}

