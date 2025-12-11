using System;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface ILoggingService
{
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogError(string message, params object[] args);
}
