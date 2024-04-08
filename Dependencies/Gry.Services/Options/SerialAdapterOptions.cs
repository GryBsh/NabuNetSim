using Gry.Adapters;

namespace Gry.Options;

public record SerialAdapterOptions : AdapterDefinition
{
    public override string Type { get; init; } = AdapterType.Serial;

    public SerialAdapterOptions()
    {
        BaudRate = 115200;
        SendBufferSize = 2;
        ReceiveBufferSize = 2;
    }
    
    public int BaudRate
    {
        get => Get<int>(nameof(BaudRate));
        set => Set(nameof(BaudRate), value);
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
}
