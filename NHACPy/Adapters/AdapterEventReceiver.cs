using Gry;
using Gry.Adapters;

namespace NHACPy.Adapters;

public class AdapterEventReceiver : IReceiver<NHACPyAdapter>
{
    public Task ReceiveAsync(string @event, NHACPyAdapter context, CancellationToken cancel)
    {
        if (@event == Adapter.Startup)
        {
            if (Path.IsPathFullyQualified(context.StoragePath) is false ||
                Path.IsPathRooted(context.StoragePath) is false)
                context.StoragePath = Path.Combine(AppContext.BaseDirectory, context.StoragePath);

            if (Directory.Exists(context.StoragePath) is false)
                Directory.CreateDirectory(context.StoragePath);
        }
        return Task.CompletedTask;
    }
}
