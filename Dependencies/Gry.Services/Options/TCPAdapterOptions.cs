using Gry.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gry.Options;

public record TCPAdapterOptions : AdapterDefinition
{
    public override string Type { get; init; } = AdapterType.TCP;

    public TCPAdapterOptions()
    {
        SendBufferSize = 8;
        ReceiveBufferSize = 8;
    }
    public bool Client
    {
        get => Get<bool>(nameof(Client));
        set => Set(nameof(Client), value);
    }
    public CancellationTokenSource? ListenerTokenSource
    {
        get => Get<CancellationTokenSource?>(nameof(ListenerTokenSource));
        set => Set(nameof(ListenerTokenSource), value);
    }
    public int SendBufferSize
    {
        get => Get<int>(nameof(SendBufferSize));
        set => Set(nameof(SendBufferSize), value);
    }
    public int ReceiveBufferSize
    {
        get => Get<int>(nameof(ReceiveBufferSize));
        set => Set(nameof(ReceiveBufferSize), value);
    }
    public bool Connection
    {
        get => Get<bool>(nameof(Connection));
        set => Set(nameof(Connection), value);
    }
}
