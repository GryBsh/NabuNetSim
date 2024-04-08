using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lgc.Options;

[Runtime.SectionName("LgcRuntime")]
public record RuntimeOptions
{
    public bool Strict { get; set; } = false;

    public bool DisableDiscovery { get; set; } = false;

    public string[] IgnoredTypes { get; set; } = [];
}
