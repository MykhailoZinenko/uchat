using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace uchat_server.Hubs;

/// <summary>
/// Global SignalR hub filter to log all invocations and surface server-side errors.
/// </summary>
public class LoggingHubFilter : IHubFilter
{
    private readonly ILogger<LoggingHubFilter> _logger;

    public LoggingHubFilter(ILogger<LoggingHubFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        _logger.LogInformation("Invoking hub method {Method} for ConnectionId={ConnectionId} Args={Args}",
            invocationContext.HubMethodName,
            invocationContext.Context.ConnectionId,
            string.Join(", ", invocationContext.HubMethodArguments.Select(a => a?.ToString() ?? "null")));

        try
        {
            var result = await next(invocationContext);
            _logger.LogInformation("Hub method {Method} completed for ConnectionId={ConnectionId}", invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hub method {Method} failed for ConnectionId={ConnectionId}", invocationContext.HubMethodName, invocationContext.Context.ConnectionId);
            throw;
        }
    }
}


