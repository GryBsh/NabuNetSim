using Nabu.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Packages;


public class PackageManager : IPackageManager
{
    readonly IConsole Log;
    public PackageManager(IConsole<PackageManager> console) { 
        Log = console;
    }

    public IEnumerable<Package> GetInstalledPackages()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<PackageSourceItem> GetAvailablePackages()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<PackageSource> GetSources()
    {
        throw new NotImplementedException();
    }

}
