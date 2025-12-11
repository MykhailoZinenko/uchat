using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Presentation.Services;

public class NotificationService : INotificationService
{
    private readonly ILoggingService _logger;

    public NotificationService(ILoggingService logger)
    {
        _logger = logger;
    }

    public void ShowSuccess(string message)
    {
        _logger.LogInformation("Success notification: {Message}", message);
        // TODO: Implement actual toast notification UI
    }

    public void ShowError(string message)
    {
        _logger.LogError("Error notification: {Message}", message);
        // TODO: Implement actual toast notification UI
    }

    public void ShowWarning(string message)
    {
        _logger.LogWarning("Warning notification: {Message}", message);
        // TODO: Implement actual toast notification UI
    }

    public void ShowInfo(string message)
    {
        _logger.LogInformation("Info notification: {Message}", message);
        // TODO: Implement actual toast notification UI
    }
}
