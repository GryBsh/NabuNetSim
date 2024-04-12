using Gry;
using Microsoft.Extensions.Logging;
using Nabu.Logs;
using Nabu.Network;
using Nabu.Settings;
using Napa;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Reactive.Linq;

namespace Nabu.Sources
{
    public class PackageService : DisposableBase
    {
        public PackageService(
            IPackageManager packages,
            ISourceService sources,
            INabuNetwork network,
            ILogger<PackageService> log,
            GlobalSettings settings
        )
        {
            Packages = packages;
            Sources = sources;
            Network = network;
            Log = log;

            Packages.Installed.CollectionChanged += InstalledChanged;

            Packages.PreservedPackages.Add("nns-bundle-classic-nabu-cycles");
            Task.Run(async () =>
            {
                await Packages.Refresh();
                Disposables.Add(
                    Observable.Interval(TimeSpan.FromMinutes(settings.PackageRefreshIntervalMinutes))
                              .Subscribe(_ => Packages.Refresh(true))
                );
            });

        }

        public ILogger<PackageService> Log { get; }
        public INabuNetwork Network { get; }
        public IPackageManager Packages { get; }
        private ConcurrentDictionary<string, DateTime> HasUpdated { get; set; } = new();
        private ISourceService Sources { get; }

        private static ProgramSource Source(SourcePackage package)
        {
            var source = new ProgramSource()
            {
                Name = package.Name,
                EnableExploitLoader = package.FeatureEnabled(AdaptorFeatures.ExploitLoader),
                EnableRetroNet = package.FeatureEnabled(AdaptorFeatures.RetroNet),
                EnableRetroNetTCPServer = package.FeatureEnabled(AdaptorFeatures.RetroNetServer),
                TCPServerPort = (int)package.Option<long>(PackageOptions.ServerPort),
                Path = package.Path,
                SourceType = SourceType.Package,
                SourcePackage = package.Id,
                Author = package.Author,
                Description = package.Description
            };
            return source;
        }

        private static ProgramSource Source(SourcePackage package, ManifestItem pak, bool mergePath = true)
        {
            var isRemotePak = NabuLib.IsHttp(pak.Path);

            var path = isRemotePak switch
            {
                false when mergePath => Path.Join(package.Path, PackageFeatures.PAKs, pak.Path),
                _ => pak.Path
            };

            var source = new ProgramSource()
            {
                Name = pak.Name ?? Path.GetFileNameWithoutExtension(pak.Path),
                EnableExploitLoader = pak.Option<bool>(AdaptorFeatures.ExploitLoader) || package.FeatureEnabled(AdaptorFeatures.ExploitLoader),
                EnableRetroNet = pak.Option<bool>(AdaptorFeatures.RetroNet) || package.FeatureEnabled(AdaptorFeatures.RetroNet),
                EnableRetroNetTCPServer = pak.Option<bool>(AdaptorFeatures.RetroNetServer) || package.FeatureEnabled(AdaptorFeatures.RetroNetServer),
                HeadlessMenu = pak.Option<bool>(AdaptorFeatures.HeadlessMenu),
                Path = path,
                SourceType = isRemotePak ? SourceType.Remote : SourceType.Local,
                SourcePackage = package.Id,
                Author = package.Author,
                Description = package.Description
            };

            return source;
        }

        private static IEnumerable<ProgramSource> SourcesFrom(SourcePackage package)
        {
            if (package.Programs.Length != 0)
            {
                yield return Source(package);
            }
            if (package.PAKs.Length != 0)
            {
                foreach (var pak in package.PAKs)
                {
                    yield return Source(package, pak);
                }
            }
            if (package.Sources.Length != 0)
            {
                foreach (var source in package.Sources)
                {
                    yield return Source(package, source);
                }
            }
        }

        private void InstalledChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var removed in e.OldItems?.Cast<SourcePackage>() ?? Array.Empty<SourcePackage>())
                {
                    var sources = SourcesFrom(removed);
                    foreach (var source in sources)
                    {
                        Sources.Remove(source);
                    }
                }
            }

            UpdateSources();
        }

        private void UpdateSources()
        {
            //Log.Write("Updating Sources from Packages");
            var needRemoteRefresh = false;
            foreach (var package in Packages.Installed)
            {
                var sources = SourcesFrom(package);
                foreach (var source in sources)
                {
                    if (source.SourceType is SourceType.Remote &&
                        !HasUpdated.TryGetValue(source.Name, out var _)
                    )
                    {
                        needRemoteRefresh = true;
                    }
                    Sources.Refresh(source);
                    HasUpdated[source.Name] = DateTime.Now;
                }
            }

            if (needRemoteRefresh)
            {
                Network.RefreshSources(RefreshType.All);
            }
        }
    }
}