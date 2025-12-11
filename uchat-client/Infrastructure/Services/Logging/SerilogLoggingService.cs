using System;
using Serilog;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Infrastructure.Services.Logging;

public class SerilogLoggingService : ILoggingService
{
    private readonly ILogger _logger;

    public SerilogLoggingService()
    {
        _logger = Log.Logger;
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.Error(message, args);
    }
}
