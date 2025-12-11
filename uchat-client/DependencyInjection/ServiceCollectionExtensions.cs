using Microsoft.Extensions.DependencyInjection;

namespace uchat_client.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUChatClient(
        this IServiceCollection services,
        string serverUrl,
        string clientId)
    {
        // Register layers in order
        services.AddInfrastructureServices(serverUrl, clientId);
        services.AddApplicationServices();
        services.AddPresentationServices();

        return services;
    }
}
