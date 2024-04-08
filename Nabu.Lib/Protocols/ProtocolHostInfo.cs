using Gry.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Protocols
{
    public class ProtocolHostInfo : IProtocolHostInfo
    {
        public string Name { get; } = $"Nabu NetSim";
        public string Description { get; } = "Nabu Network Simulator";
        public string Version { get; } = Emulator.Version;
    }
}
