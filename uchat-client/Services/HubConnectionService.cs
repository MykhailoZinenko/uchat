using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace uchat_client.Services;

public class HubConnectionService : IHubConnectionService
{
    private readonly HubConnection _connection;

    public HubConnection Connection => _connection;
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public HubConnectionService(string serverUrl)
    {
        Console.WriteLine($"[HubConnection] Initializing connection to {serverUrl}/chat");
        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/chat")
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += async (error) =>
        {
            Console.WriteLine($"[HubConnection] Connection closed: {error?.Message ?? "No error"}");
            await Task.CompletedTask;
        };

        _connection.Reconnecting += (error) =>
        {
            Console.WriteLine($"[HubConnection] Reconnecting: {error?.Message ?? "No error"}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            Console.WriteLine($"[HubConnection] Reconnected with ID: {connectionId}");
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            Console.WriteLine("[HubConnection] Starting connection...");
            await _connection.StartAsync();
            Console.WriteLine($"[HubConnection] Connection started. State: {_connection.State}");
        }
        else
        {
            Console.WriteLine($"[HubConnection] Connection already in state: {_connection.State}");
        }
    }

    public async Task StopAsync()
    {
        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.StopAsync();
        }
    }

    public async Task<T> InvokeAsync<T>(string methodName, params object[] args)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Connection is not established");
        }

        Console.WriteLine($"[HubConnection] Invoke {methodName} args=[{string.Join(", ", args.Select(a => a ?? "null"))}]");
        return await _connection.InvokeCoreAsync<T>(methodName, args);
    }
}
