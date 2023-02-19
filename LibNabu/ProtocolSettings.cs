using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu;

public record ProtocolSettings
{
    public string Path { get; set; } = string.Empty;
    public byte[] Commands { get; set; }
    public List<string> Modules { get; set; } = new();

}
