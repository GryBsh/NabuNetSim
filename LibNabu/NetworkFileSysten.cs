using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network;

public class Paths
{
    public string Files { get; set; } = "./Files";
    public string PAKs { get; set; } = "./PAKs";
    public string NABUs { get; set; } = "./NABUs";
    public string Cache { get; set; } = "./cache";
    public string MAMEPath { get; set; } = "./MAME";
}

public class NetworkFileSystem
{
    Settings Settings { get; }
    
    Paths Paths { get; }

    public NetworkFileSystem(
        Settings settings, 
        Paths paths
    )
    {
        Settings = settings;
        Paths = paths;
    }

  


}
