using System.ComponentModel.DataAnnotations;

namespace uchat_server.Configuration;

public class JwtSettings
{
    [Required(ErrorMessage = "JWT AccessSecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT AccessSecretKey must be at least 32 characters long")]
    public string AccessSecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT RefreshSecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT RefreshSecretKey must be at least 32 characters long")]
    public string RefreshSecretKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "JWT AccessTokenLifetimeMs must be greater than 0")]
    public int AccessTokenLifetimeMs { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "JWT RefreshTokenLifetimeMs must be greater than 0")]
    public int RefreshTokenLifetimeMs { get; set; }
}
