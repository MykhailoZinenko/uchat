using System;
using Microsoft.AspNetCore.SignalR.Client;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Infrastructure.Services.SignalR;

public class RetryPolicy : IRetryPolicy
{
    private readonly TimeSpan[] _retryDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30)
    };

    private readonly ILoggingService _logger;

    public RetryPolicy(ILoggingService logger)
    {
        _logger = logger;
    }

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        TimeSpan delay;

        if (retryContext.PreviousRetryCount >= _retryDelays.Length)
        {
            delay = _retryDelays[^1];
        }
        else
        {
            delay = _retryDelays[retryContext.PreviousRetryCount];
        }

        _logger.LogInformation("SignalR retry #{RetryCount} - waiting {Delay}s before reconnect",
            retryContext.PreviousRetryCount + 1, delay.TotalSeconds);

        return delay;
    }
}
