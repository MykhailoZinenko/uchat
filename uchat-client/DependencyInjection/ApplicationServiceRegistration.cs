using Microsoft.Extensions.DependencyInjection;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Features.Authentication.ViewModels;
using uchat_client.Core.Application.Features.Chat.ViewModels;
using uchat_client.Core.Application.Features.Contacts.ViewModels;
using uchat_client.Core.Application.Features.Settings.ViewModels;
using uchat_client.Core.Application.Features.Shell.ViewModels;
using uchat_client.Core.Application.Services.Validation;

namespace uchat_client.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MyAccountViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<SoundNotificationsViewModel>();
        services.AddTransient<ContactsViewModel>();
        services.AddTransient<AddContactViewModel>();
        services.AddSingleton<MainViewModel>(); // Singleton - main shell
        services.AddSingleton<SidebarViewModel>(); // Singleton - shared sidebar

        // Register application services
        services.AddSingleton<IValidationService, ValidationService>();

        return services;
    }
}
