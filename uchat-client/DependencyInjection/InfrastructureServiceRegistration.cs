using Microsoft.Extensions.DependencyInjection;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Infrastructure.Services;
using uchat_client.Infrastructure.Services.Authentication;
using uchat_client.Infrastructure.Services.Logging;
using uchat_client.Infrastructure.Services.Messaging;
using uchat_client.Infrastructure.Services.SignalR;
using uchat_client.Infrastructure.Services.Storage;

namespace uchat_client.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string serverUrl,
        string clientId)
    {
        // Logging - must be registered first as other services depend on it
        services.AddSingleton<ILoggingService, SerilogLoggingService>();

        // Session Storage
        services.AddSingleton<ISessionStorageService>(sp =>
        {
            var storage = new SessionStorageService();
            storage.Initialize(clientId);
            return storage;
        });

        // SignalR
        services.AddSingleton<IHubConnectionService>(sp =>
            new HubConnectionService(serverUrl, sp.GetRequiredService<ILoggingService>()));

        // Authentication
        services.AddSingleton<IAuthService, AuthService>();

        // Messaging
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IContactService, ContactService>();
        services.AddSingleton<IRoomService, RoomService>();

        // Theme
        services.AddSingleton<IThemeService, ThemeService>();

        return services;
    }
}
