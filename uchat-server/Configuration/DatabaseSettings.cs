using System.ComponentModel.DataAnnotations;

namespace uchat_server.Configuration;

public class DatabaseSettings
{
    [Required(ErrorMessage = "Database ConnectionString is required")]
    [MinLength(1, ErrorMessage = "Database ConnectionString cannot be empty")]
    public string ConnectionString { get; set; } = string.Empty;
}
