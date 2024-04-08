using Gry.Adapters;
using Gry.Options;
using Lgc;

namespace NHACPy.Options
{
    [Runtime.SectionName("Server")]
    public record ServerOptions : AdapterServerOptions<
        AdapterDefinition, 
        TCPAdapterOptions, 
        SerialAdapterOptions
    >, IDependencyOptions;
}
