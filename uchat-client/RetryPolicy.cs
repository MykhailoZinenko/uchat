using Microsoft.AspNetCore.SignalR.Client;

namespace uchat_client;

public class RetryPolicy : IRetryPolicy
{
    private readonly TimeSpan[] _retryDelays =
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30)
    };

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

        Console.WriteLine($"[DEBUG] SignalR retry #{retryContext.PreviousRetryCount + 1} - waiting {delay.TotalSeconds}s before reconnect");

        return delay;
    }
}
