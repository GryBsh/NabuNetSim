using Nabu.Network;

namespace Nabu.Configuration
{
    public interface IEmulatorSettings
    {
        IList<SerialAdaptorSettings> Serial { get; set; }
        IList<TCPAdaptorSettings> TCP { get; set; }
        IList<ProgramSource> Sources { get; set; }
        ILogSettings Logs { get; set; }
        IStorageSettings Storage { get; set; }
        IDatabaseSettings Database { get; set; }
        IPackageSettings Packages { get; set; }
        IExtensionSettings Extensions { get; set; }
    }
}