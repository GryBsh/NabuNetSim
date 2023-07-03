using Nabu.Network;

namespace Nabu.Configuration
{
    public interface IExtensionSettings
    {
        List<string> EnabledTypes { get; set; }
        List<ProtocolSettings> Protocols { get; set; }
    }
}