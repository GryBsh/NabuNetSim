using Gry.Adapters;
using Newtonsoft.Json;
using Gry.Settings;

namespace Nabu.Settings;

[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
public record SerialAdaptorSettings : AdaptorSettings
{
    public override string Type { get; init; } = AdapterType.Serial;

    public SerialAdaptorSettings()
    {
        BaudRate = 115200;
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    [Setting("Baud Rate", Section = "Port")]
    public int BaudRate
    {
        get => Get<int>(nameof(BaudRate));
        set => Set(nameof(BaudRate), value);
    }
}