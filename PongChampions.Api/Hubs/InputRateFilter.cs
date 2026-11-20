using Microsoft.AspNetCore.SignalR;

namespace PongChampions.Api.Hubs;

public sealed class InputRateFilter(TimeProvider clock) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocation, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        const string key = "input_rate";
        var now = clock.GetUtcNow();

        if (invocation.Context.Items.TryGetValue(key, out var stored) && stored is Window window &&
            now - window.Start < TimeSpan.FromSeconds(1))
        {
            if (++window.Count > 60)
                throw new HubException("Input rate exceeded. Try again shortly.");
        }
        else
            invocation.Context.Items[key] = new Window(now);

        return await next(invocation);
    }

    private sealed class Window(DateTimeOffset start)
    {
        public DateTimeOffset Start { get; } = start;
        public int Count { get; set; } = 1;
    }
}
