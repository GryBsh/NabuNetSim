using Gry;
using Gry.Adapters;
using Gry.Settings;

namespace NHACPy.Adapters;

public class AdapterEventReceiver(ILocationService location) : IReceiver<NHACPyAdapter>
{
    public Task ReceiveAsync(string @event, NHACPyAdapter context, CancellationToken cancel)
    {
        if (@event == Adapter.Startup)
        {
            context.StoragePath = Path.Combine(location.GetPath("storage", context.Name), context.StoragePath);

            if (Directory.Exists(context.StoragePath) is false)
                Directory.CreateDirectory(context.StoragePath);
        }
        return Task.CompletedTask;
    }
}
