using System.ComponentModel.DataAnnotations;

namespace uchat_server.Configuration;

public class SessionSettings
{
    [Range(1, long.MaxValue, ErrorMessage = "SessionTokenLifetimeMs must be greater than 0")]
    public long SessionTokenLifetimeMs { get; set; }
}
