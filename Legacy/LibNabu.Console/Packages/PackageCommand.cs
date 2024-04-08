using Napa;
using Spectre.Console.Cli;

namespace Nabu.Cli
{
    public abstract class PackageCommand<T> : AsyncCommand<T> where T : CommandSettings
    {
        protected PackageCommand(IPackageManager packages)
        {
            Packages = packages;
        }

        protected IPackageManager Packages { get; }
    }
}