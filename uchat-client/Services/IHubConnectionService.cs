using Microsoft.AspNetCore.SignalR.Client;

namespace uchat_client.Services;

public interface IHubConnectionService
{
    HubConnection Connection { get; }
    bool IsConnected { get; }
    Task StartAsync();
    Task StopAsync();
    Task<T> InvokeAsync<T>(string methodName, params object[] args);
}
