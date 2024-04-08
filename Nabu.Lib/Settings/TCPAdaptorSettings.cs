using Gry.Adapters;
using Newtonsoft.Json;

namespace Nabu.Settings;

[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record TCPAdaptorSettings : AdaptorSettings
{
    public override string Type { get; init; } = AdapterType.TCP;

    public TCPAdaptorSettings()
    {
        SendBufferSize = 8;
        ReceiveBufferSize = 8;
    }

    public CancellationTokenSource? ListenTask
    {
        get => Get<CancellationTokenSource?>(nameof(ListenTask));
        set => Set(nameof(ListenTask), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int SendBufferSize
    {
        get => Get<int>(nameof(SendBufferSize));
        set => Set(nameof(SendBufferSize), value);
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int ReceiveBufferSize
    {
        get => Get<int>(nameof(ReceiveBufferSize));
        set => Set(nameof(ReceiveBufferSize), value);
    }

    [JsonIgnore]
    public bool Connection
    {
        get => Get<bool>(nameof(Connection));
        set => Set(nameof(Connection), value);
    }

    [JsonIgnore]
    public int PortNumber
    {
        get => Get<int>(nameof(Port));
        set => Set(nameof(Port), value);
    }
}
