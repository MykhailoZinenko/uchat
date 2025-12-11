using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Infrastructure.Services.SignalR;

public class HubConnectionService : IHubConnectionService
{
    private readonly HubConnection _connection;
    private readonly ILoggingService _logger;

    public HubConnection Connection => _connection;
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public HubConnectionService(string serverUrl, ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Initializing SignalR connection to {ServerUrl}/chat", serverUrl);

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/chat")
            .WithAutomaticReconnect(new RetryPolicy(logger))
            .Build();

        _connection.Closed += async (error) =>
        {
            if (error != null)
            {
                _logger.LogError(error, "SignalR connection closed with error");
            }
            else
            {
                _logger.LogInformation("SignalR connection closed");
            }
            await Task.CompletedTask;
        };

        _connection.Reconnecting += (error) =>
        {
            if (error != null)
            {
                _logger.LogWarning("SignalR reconnecting due to error: {Error}", error.Message);
            }
            else
            {
                _logger.LogInformation("SignalR reconnecting");
            }
            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            _logger.LogInformation("Starting SignalR connection");
            await _connection.StartAsync();
            _logger.LogInformation("SignalR connection started. State: {State}", _connection.State);
        }
        else
        {
            _logger.LogDebug("SignalR connection already in state: {State}", _connection.State);
        }
    }

    public async Task StopAsync()
    {
        if (_connection.State == HubConnectionState.Connected)
        {
            _logger.LogInformation("Stopping SignalR connection");
            await _connection.StopAsync();
        }
    }

    public async Task<T> InvokeAsync<T>(string methodName, params object[] args)
    {
        if (!IsConnected)
        {
            _logger.LogError("Cannot invoke {MethodName}: Connection is not established", methodName);
            throw new InvalidOperationException("Connection is not established");
        }

        _logger.LogDebug("Invoking SignalR method: {MethodName}", methodName);
        return await _connection.InvokeCoreAsync<T>(methodName, args);
    }
}
