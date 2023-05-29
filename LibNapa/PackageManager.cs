using Nabu;
using Nabu.Network;
using Nabu.Packages;
using Nabu.Services;
using System.IO;
using System.IO.Compression;
namespace Napa;

public record FoundPackage(string Path, Package? Package = null);

public class PackageManager : IPackageManager
{
    const string ArchiveFolderName = "archive";
    const string ArchiveNameFormat = "yyyy-MM-dd-HH-mm-ss";
    const string ManifestFileExtension = ".yaml";
    const string ManifestFilename = $"napa{ManifestFileExtension}";
    const string PackageFileExtension = ".napa";

    static SemaphoreSlim UpdateInstalledLock { get; } = new(1, 1);

    ILog Log { get; }

    Settings Settings { get; }
    IFileCache Cache { get; }
    IHttpCache Http { get; }

    SourceService SourceService { get; }
    public string PackageFolder { get; } = Path.Combine(AppContext.BaseDirectory, "Packages");
    public string ArchiveFolder => Path.Combine(PackageFolder, ArchiveFolderName);

    public IList<PackageSource> Sources => Settings.PackageSources;

    public IEnumerable<SourcePackage> Installed { get; private set; } = Array.Empty<SourcePackage>();

    public IEnumerable<SourcePackage> Available { get; private set; } = Array.Empty<SourcePackage>();

    public PackageManager(
        ILog<PackageManager> console,
        Settings settings,
        IFileCache cache,
        IHttpCache http,
        SourceService sources
    )
    {
        Log = console;
        Settings = settings;
        Cache = cache;
        Http = http;
        SourceService = sources;

        NabuLib.EnsureFolder(PackageFolder);
        Task.Run(Refresh);
    }

    public Task Refresh()
    {
        Log.Write("Refreshing Packages");
        return Task.WhenAll(
            UpdateInstalled(),
            UpdateAvailable()
        );
    }

    public static ProgramSource Source(InstalledPackage package)
    {
        var source = new ProgramSource()
        {
            Name = package.Name,
            EnableExploitLoader = package.FeatureEnabled(AdaptorFeatures.ExploitLoader),
            EnableRetroNet = package.FeatureEnabled(AdaptorFeatures.RetroNet),
            EnableRetroNetTCPServer = package.FeatureEnabled(AdaptorFeatures.RetroNetServer),
            Path = package.Path,
            SourceType = SourceType.Package
        };
        return source;
    }

    public static ProgramSource Source(InstalledPackage package, ManifestItem pak, bool mergePath = true)
    {
        var isRemotePak = NabuLib.IsHttp(pak.Path);

        var path = isRemotePak switch
        {
            false when mergePath => Path.Join(package.Path, PackageFeatures.PAKs, pak.Path),
            _ => pak.Path
        };

        var source = new ProgramSource()
        {
            Name = pak.Name,
            EnableExploitLoader = pak.Option<bool>(AdaptorFeatures.ExploitLoader) is true,
            EnableRetroNet = pak.Option<bool>(AdaptorFeatures.RetroNet) is true,
            EnableRetroNetTCPServer = pak.Option<bool>(AdaptorFeatures.RetroNetServer) is true,
            Path = path,
            SourceType = isRemotePak ? SourceType.Remote : SourceType.Local
        };
        return source;
    }

    async Task<FoundPackage> LoadManifest(string path)
    {
        if (!Path.Exists(path))
            return new(path, null);
        
        try
        {
            var package = (await Yaml.Deserialize<Package>(path, Cache)).FirstOrDefault();
            return new(path, package);
        }
        catch (Exception ex)
        {
            Log.WriteError($"Can't load {path}", ex);
        }
        return new(path, null);
    }

    Task<FoundPackage> LoadManifestFrom(string path, string? name = null)
    {
        path = name switch
        {
            null when Path.GetExtension(name) is ManifestFileExtension => path,
            null => Path.Join(path, ManifestFilename),
            _ => Path.Join($"{name}{ManifestFileExtension}")
        };

        return LoadManifest(path);
    }

    async Task<FoundPackage> LoadPackage(string path)
    {
        if (!Path.Exists(path))
            return new(path, null);

        var tmpFile = Path.GetTempFileName();
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );

            using var archive = new ZipArchive(
                stream,
                ZipArchiveMode.Read,
                false
            );

            var napaEntry = archive.GetEntry(ManifestFilename);
            if (napaEntry is null)
                return new(path, null);

            napaEntry.ExtractToFile(tmpFile, true);

            var found = await LoadManifest(tmpFile) with { Path = path };
            File.Delete(tmpFile);
            return found;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Empty, ex);
            return new(path, null);
        }
    }
    async Task<FoundPackage> LoadPackageFrom(string path, string? name = null)
    {
        path = name switch
        {
            null when Path.GetExtension(name) is PackageFileExtension => path,
            null => string.Empty,
            _ => Path.Join(path, $"{name}{PackageFileExtension}")
        };

        if (path == string.Empty) return new(path, null);
        return await LoadPackage(path);
    }


    public async Task<FoundPackage> Open(string folder, string? name = null)
    {
        var found = await LoadPackageFrom(folder, name);

        return found.Package is not null ?
               found :
               await LoadManifestFrom(folder, name);
    }

    IEnumerable<string> Strings(params string[] strings) => strings;

    bool IsHigher(string higher, string lower)
    {
        return higher != lower && higher == Strings(higher, lower).OrderDescending().First();
    }

    public (bool, Package) UninstallPackage(Package package, Package newPackage)
    {
        if (package is null) return (true, newPackage);

        Log.WriteWarning($"Uninstalling {package.Name}[{package.Id}] {package.Version}");

        if (IsHigher(newPackage.Version, package.Version))
        {
            return (UninstallPackage(package), newPackage);
        }
        else
        {
            Log.WriteWarning($"Existing package {package.Name}[{package.Id}] is newer.");
            Log.WriteVerbose($"{package.Name}[{package.Id}] {package.Version} > {newPackage.Version}");
            return (false, package);
        }
    }

    string ArchiveFileName(string path, string name, string discriminator, string ext, bool withTimestamp = false)
    {
        var filename = $"{Path.GetFileNameWithoutExtension(name)}-{discriminator}";

        filename = withTimestamp switch
        {

            true => $"{filename}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}{ext}",
            false => $"{filename}{ext}"
        };
        return Path.Combine(path, filename);
    }

    string PackagePath(Package package)
        => Path.Join(PackageFolder, package.Id);
    string ArchivePath(Package package)
        => Path.Join(ArchiveFolder, package.Id);

    public bool UninstallPackage(Package package)
    {
        try
        {
            var path = PackagePath(package); 
            var archiveFolder = ArchivePath(package);

            if (!Directory.Exists(archiveFolder))
                Directory.CreateDirectory(archiveFolder);

            var archiveFile = ArchiveFileName(archiveFolder, package.Id, package.Version, PackageFileExtension, true);
            ZipFile.CreateFromDirectory(path, archiveFile);

            Directory.Delete(path, true);
            return true;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Empty, ex);
        }
        return false;
    }

    public async Task<Package?> InstallPackage(string path)
    {
        var pkgPath = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        var (fndPath, newPackage) = await Open(pkgPath!, name);
        if (newPackage is null)
        {
            Log.WriteError($"Cannot open {path}");
            return null;
        }


        var destinationFolder = PackagePath(newPackage);

        if (Directory.Exists(destinationFolder))
        {
            var (_, package) = await Open(destinationFolder);
            if (package is null)
                Directory.Delete(destinationFolder, true);
            else
            {
                (var complete, package) = UninstallPackage(package, newPackage);
                if (!complete)
                    return package;
            }

        }

        Log.Write($"Installing package {newPackage.Name} [{newPackage.Id}:{newPackage.Version}]");
        try
        {
            Directory.CreateDirectory(destinationFolder);
            ZipFile.ExtractToDirectory(fndPath, destinationFolder);
            File.Delete(fndPath);

            return newPackage;
        }
        catch (Exception ex)
        {
            Log.WriteError(string.Empty, ex);
            return null;
        }
    }

    public async Task<bool> CreatePackage(string path, string destination)
    {
        var (_, package) = await Open(path);
        if (package is null) return false;
        
        var napa = Path.Combine(destination, $"{Path.GetFileName(path)}{PackageFileExtension}");
        ZipFile.CreateFromDirectory(path, napa);
        return true;
    }

    public async Task UpdateInstalled()
    {
        
        await UpdateInstalledLock.WaitAsync();

        var result = new List<InstalledPackage>();
        var loosePackages = Directory.GetFiles(PackageFolder, "*.napa");

        foreach (var loosePackage in loosePackages)
        {
            await InstallPackage(loosePackage);
        }
        

        var packageFolders = Directory.GetDirectories(PackageFolder);
        foreach (var packageFolder in packageFolders)
        {
            var (_, package) = await Open(packageFolder);

            if (package is null)
                continue;

            var sourcePackage = new InstalledPackage(package, packageFolder);

            if (package.Programs.Any())
            {
                SourceService.Refresh(Source(sourcePackage));
            }
            if (package.PAKs.Any())
            {
                foreach (var pak in package.PAKs)
                {
                    SourceService.Refresh(Source(sourcePackage, pak));
                }
            }
            if (package.Sources.Any())
            {
                foreach (var source in package.Sources)
                {
                    SourceService.Refresh(Source(sourcePackage, source));
                }
            }
            result.Add(sourcePackage);

        }
        Installed = result;

        UpdateInstalledLock.Release();
    }

    public async Task UpdateAvailable()
    {
        var result = new List<SourcePackage>();
        var sources = Settings.PackageSources;
        foreach (var src in sources)
        {

            var http = NabuLib.IsHttp(src.Path) ? Http : null;
            var list = await Yaml.Deserialize<SourcePackage>(src.Path, Cache, http);
            foreach (var package in list)
            {
                var path = package.Path.Trim('/', '\\');
                if (NabuLib.IsHttp(path))
                    path = path.Replace('\\', '/');
                path = Path.Combine(src.Path, path);
                result.Add(new(package, src.Name, path));
            }
        }

        Available = result;
    }

    public IEnumerable<SourcePackage> AvailablePackages(PackageSource source)
    {
        return Available.Where(p => p.Source == source.Name);
    }

    public IEnumerable<SourcePackage> AvailableUpdates()
    {
        return from i in Installed
               from p in Available
               let versions = from v in Strings(p.Version, i.Version) orderby v descending select v
               let highest = versions.First()
               where p.Version == highest
               select p;
    }
}
