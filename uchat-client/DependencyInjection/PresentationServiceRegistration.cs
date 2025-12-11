using Microsoft.Extensions.DependencyInjection;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Presentation.Services;

namespace uchat_client.DependencyInjection;

public static class PresentationServiceRegistration
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // UI Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INotificationService, NotificationService>();

        return services;
    }
}
