using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IHubConnectionService
{
    HubConnection Connection { get; }
    bool IsConnected { get; }
    Task StartAsync();
    Task StopAsync();
    Task<T> InvokeAsync<T>(string methodName, params object[] args);
}
