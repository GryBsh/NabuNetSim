using Gry;
using Gry.Caching;
using Gry.Serialization;
using Gry.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Compression;
using YamlDotNet.Serialization;

namespace Napa;

public class PackageManager : IPackageManager
{
    private const string ArchiveFolderName = "archive";
    private const string JSONFileExtension = ".json";
    private const string JSONManifestFilename = $"napa{JSONFileExtension}";
    private const string ManifestFilename = $"napa{YAMLFileExtension}";
    private const string PackageFileExtension = ".napa";
    private const string YAMLFileExtension = ".yaml";

    private YAMLSerializer Yaml = new YAMLSerializer();

    public PackageManager(
        ILogger<PackageManager> console,
        IOptions<NapaOptions> settings,
        IFileCache cache,
        IHttpCache http,        ILocationService location
    )
    {
        Log = console;
        Settings = settings.Value;
        Cache = cache;
        Http = http;        Location = location;        NapaLib.EnsureFolder(PackageFolder);
    }

    public string ArchiveFolder => Path.Combine(PackageFolder, ArchiveFolderName);
    public ObservableCollection<SourcePackage> Available { get; private set; } = new();
    public ObservableCollection<InstalledPackage> Installed { get; private set; } = new();
    public ConcurrentQueue<string> InstallQueue { get; } = new();
    public string PackageFolder => Location.GetPath("packages");
    public List<string> PreservedPackages { get; } = new();
    public List<PackageSource> Sources => Settings.Sources;
    public ConcurrentQueue<string> UninstallQueue { get; } = new();
    private static SemaphoreSlim RefreshLock { get; } = new(1, 1);
    private IFileCache Cache { get; }
    private IHttpCache Http { get; }    private ILocationService Location { get; }    private ILogger Log { get; }
    
    private NapaOptions Settings { get; }
    private List<SourcePackage> SourcePackages { get; } = [];

    public async Task<bool> Create(string path, string destination)
    {
        var (_, package, _) = await Open(path);
        if (package is null) return false;

        var napa = Path.Combine(destination, $"{Path.GetFileName(path)}{PackageFileExtension}");
        ZipFile.CreateFromDirectory(path, napa);
        return true;
    }

    public async Task<PackageInfo> Install(string path, bool force = false)
    {
        var pkgPath = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var (fndPath, newPackage, type) = await Open(pkgPath, name);
        if (newPackage is null)
        {
            Log.LogError($"Cannot open {path}");
            return new(path, null);
        }

        var destinationFolder = PackagePath(newPackage);

        if (Directory.Exists(destinationFolder))
        {
            //var (oldPath, package, _) = await Open(destinationFolder);
            var package = Installed.FirstOrDefault(i => i.Id.LowerEquals(newPackage.Id));
            if (package is null)
                Directory.Delete(destinationFolder, true);
            else
            {
                (var complete, _) = Uninstall(package, newPackage);
                if (!complete)
                {
                    Log.LogError($"Failed to uninstall {package.Name}[{package.Id}]");
                    return new(package.ManifestPath, package);
                }
            }
        }

        Log.LogInformation($"Installing package {newPackage.Name} [{newPackage.Id}:{newPackage.Version}]");
        try
        {
            Directory.CreateDirectory(destinationFolder);
            if (type is PackageType.Napa)
                ZipFile.ExtractToDirectory(fndPath, destinationFolder);
            else if (type is PackageType.Folder)
            {
                var srcPath = Path.GetDirectoryName(fndPath);
                var files = Directory.GetFiles(srcPath!, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var destPath = Path.GetRelativePath(fndPath, file);
                    File.Copy(file, Path.Join(destinationFolder, destPath), true);
                }
            }
            Log.LogInformation($"Installed package {newPackage.Name} [{newPackage.Id}:{newPackage.Version}]");

            var newManifest = Path.Join(destinationFolder, JSONManifestFilename);
            var installed = new InstalledPackage(newPackage, destinationFolder, newManifest);

            Installed.Add(installed);

            return new(fndPath, installed);
        }
        catch (Exception ex)
        {
            Log.LogError(string.Empty, ex);
            return new(fndPath, null);
        }
    }

    public async Task<PackageInfo> Open(string folder, string? name = null)
    {
        var found = await LoadPackageFrom(folder, name);

        return found.Found ?
               found :
               await LoadManifestFrom(folder, name);
    }

    public async Task Refresh(bool silent = false, bool localOnly = false)
    {
        if (RefreshLock.CurrentCount == 0)
            return;

        if (!silent)
            Log.LogInformation("Refreshing Packages");

        await RefreshLock.WaitAsync();
        var installed = await UpdateInstalled();
        await UpdateAvailable(installed);
        RefreshLock.Release();

    }

    public Task Uninstall(string id)
    {
        try
        {
            var package = Installed.FirstOrDefault(x => x.Id == id);
            if (package is null)
                return Task.CompletedTask;

            Uninstall(package);
        }
        catch (Exception ex)
        {
            Log.LogError(string.Empty, ex);
        }
        return Task.CompletedTask;
    }

    public bool Uninstall(InstalledPackage package)
    {
        static string ArchiveFileName(
            string path,
            string name,
            string discriminator,
            string ext,
            bool withTimestamp = false
        ) {
            var filename = $"{Path.GetFileNameWithoutExtension(name)}-{discriminator}";

            filename = withTimestamp switch
            {
                true => $"{filename}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}{ext}",
                false => $"{filename}{ext}"
            };
            return Path.Combine(path, filename);
        }

        try
        {
            var path = PackagePath(package);
            var archiveFolder = Path.Join(ArchiveFolder, package.Id);

            if (!Directory.Exists(archiveFolder))
                Directory.CreateDirectory(archiveFolder);

            var archiveFile = ArchiveFileName(archiveFolder, package.Id, package.Version, PackageFileExtension, true);
            ZipFile.CreateFromDirectory(path, archiveFile);
            Log.LogWarning($"Uninstalling {PackageLogName(package)}  {package.Version}");
            Directory.Delete(path, true);

            var installed = Installed.FirstOrDefault(i => i.Id.LowerEquals(package.Id));
            if (installed != null)
            {
                Installed.Remove(installed);
                Cache.UnCachePath(installed.Path);
            }

            Log.LogWarning($"Uninstalled {PackageLogName(package)} {package.Version}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogError(ex.Message);
        }
        return false;
    }

    public (bool, Package) Uninstall(InstalledPackage package, Package newPackage)
    {
        if (package is null) return (true, newPackage);

        if (IsHigher(newPackage.Version, package.Version))
        {
            return (Uninstall(package), newPackage);
        }
        else
        {
            Log.LogWarning($"Existing package {PackageLogName(package)} is newer.");
            Log.LogDebug($"{PackageLogName(package)} {package.Version} > {newPackage.Version}");
            return (false, package);
        }
    }

    private async Task<T?> DeserializeJson<T>(string path)
    {
        var cached = NapaLib.IsHttp(path) ?
                        await Http.GetBytes(path) :
                        await Cache.GetBytes(path);
        //await File.ReadAllBytesAsync(path);

        var reader = new JsonTextReader(
            new StreamReader(
                new MemoryStream(cached.ToArray())
            )
        );

        var result = typeof(T).IsArray switch
        {
            true => JArray.Load(reader).ToObject<T>(),
            _ => JObject.Load(reader).ToObject<T>()
        };
        return result;
    }

    private async IAsyncEnumerable<InstalledPackage> GetInstalledPackages()
    {
        var packageFolders = Directory.GetDirectories(PackageFolder);
        foreach (var packageFolder in packageFolders)
        {
            var (manifest, package, _) = await Open(packageFolder);

            if (package is null)
                continue;

            yield return new InstalledPackage(package, packageFolder, manifest);
        }
    }

    private async Task<SourcePackage[]?> GetPackagesFrom(string uri)
    {
        var isJson = Path.GetExtension(uri) is JSONFileExtension;
        var http = NapaLib.IsHttp(uri) ? Http : null;
        try
        {
            var result = isJson switch
            {
                true => await DeserializeJson<SourcePackage[]>(uri),
                false => Yaml.Deserialize<SourcePackage>(new SerializerOptions(), new StringReader(await Cache.GetString(uri)))
            };
            return result;
        }
        catch (Exception ex)
        {
            Log.LogDebug(ex, "Error deserializing manifest");
        }

        return [];
    }

    private bool IsHigher(string higher, string lower)
    {
        return higher != lower &&
               higher == new[] { higher, lower }.OrderDescending().First();
    }

    private async Task<PackageInfo> LoadManifest(string path)
    {
        if (!Path.Exists(path))
            return new(path, null);

        try
        {
            var package = path switch
            {
                _ when Path.GetExtension(path) is JSONFileExtension 
                    => await DeserializeJson<Package>(path),
                _ when Path.GetExtension(path) is YAMLFileExtension 
                    =>  Yaml.Deserialize<SourcePackage>(
                            new SerializerOptions(), 
                            new StringReader(await Cache.GetString(path))
                        ).FirstOrDefault(),
                _ => throw new ArgumentException("Manifest type not supported", nameof(path))
            };

            return new(path, package);
        }
        catch (Exception ex)
        {
            Log.LogError($"Can't load {path}", ex);
        }
        return new(path, null);
    }

    private async Task<PackageInfo> LoadManifestFrom(string path, string? name = null)
    {
        var ext = Path.GetExtension(name);
        var jsonManifest = Path.Join(path, JSONManifestFilename);
        var yamlManifest = Path.Join(path, ManifestFilename);
        var namedManifest = Path.Join(path, $"{name}{YAMLFileExtension}");

        path = name switch
        {
            null when ext is YAMLFileExtension or JSONFileExtension => path,
            null when Path.Exists(jsonManifest) => jsonManifest,
            null when Path.Exists(yamlManifest) => yamlManifest,
            _ => namedManifest
        };

        //Log.LogInformation($"Manifest Path: {path}");
        return await LoadManifest(path) with { Type = PackageType.Folder };
    }

    private async Task<PackageInfo> LoadPackageFrom(string path, string? name = null)
    {
        path = name switch
        {
            null when Path.GetExtension(path) is PackageFileExtension => path,
            null => string.Empty,
            _ => Path.Join(path, $"{name}{PackageFileExtension}")
        };

        if (path == string.Empty) return new(path, null);

        if (!Path.Exists(path))
            return new(path, null);

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

            var jsonEntry = archive.GetEntry(JSONManifestFilename);
            var yamlEntry = archive.GetEntry(ManifestFilename);

            if (jsonEntry is null && yamlEntry is null)
                return new(path, null);

            var napaEntry = jsonEntry is not null ? jsonEntry : yamlEntry;
            if (napaEntry is null)
                return new(path, null);

            var ext = Path.GetExtension(napaEntry.Name);
            var tmpFile = Path.GetTempFileName() + ext;

            napaEntry.ExtractToFile(tmpFile, true);

            //Log.LogInformation($"Package Manifest Path: {tmpFile}");

            var found = await LoadManifest(tmpFile) with { Path = path, Type = PackageType.Napa };
            File.Delete(tmpFile);
            return found;
        }
        catch (Exception ex)
        {
            Log.LogError(string.Empty, ex);
            return new(path, null);
        }

        //return await LoadPackage(path);
    }

    private string PackageLogName(Package package)
        => $"{package.Name} [{package.Id}]";

    private string PackagePath(Package package)
        => PackagePath(package.Id);

    private string PackagePath(string id)
        => Path.Join(PackageFolder, id);

    private async Task<string?> StagePackage(string path)
    {
        if (NapaLib.IsHttp(path))
        {
            path = await Http.GetFile(path, true) ?? string.Empty;
            if (path == string.Empty)
                return null;
        }
        var name = Path.GetFileName(path);
        var destination = Path.Join(PackageFolder, name);
        File.Copy(path, destination, true);
        return destination;
    }

    private Task<string?> StagePackageId(string id)
    {
        var available = Available.Where(p => p.Id == id).FirstOrDefault();
        if (available is null)
            return Task.FromResult<string?>(null);
        return StagePackage(available.Path);
    }

    private async Task UpdateAvailable(IEnumerable<SourcePackage> installed)
    {
        //if (locked)
        //    await UpdateLock.WaitAsync();
        var queued = InstallQueue.ToArray();
        var result = new List<SourcePackage>();
        await UpdateSourcePackages();
        result.AddRange(
            from i in installed
            from p in SourcePackages
            where i.Id == p.Id
            where p.Version != i.Version
            let versions = from v in new[] { p.Version, i.Version } orderby v descending select v
            let highest = versions.First()
            where p.Version == highest
            where queued.Any(i => i.LowerEquals(p.Id)) is false
            select p
        );
        result.AddRange(
            from p in SourcePackages
            where installed.Any(i => i.Id.LowerEquals(p.Id)) is false
            select p
        );

        Available.SetItems(result);

        //if (locked)
        //     UpdateLock.Release();
    }

    /// <summary>
    ///     - Uninstall Packages
    ///     - Stage Packages for Install
    ///     - Install Packages
    ///     - Refresh Installed Package List
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<InstalledPackage>> UpdateInstalled(bool locked = false)
    {
        //if (locked)
        //    await UpdateLock.WaitAsync();

        var result = new List<InstalledPackage>();
        var forceRemoval = Enumerable.Concat(Settings.UninstallPackages, Settings.UninstallPackageIds);
        foreach (var uninstalled in forceRemoval)
            UninstallQueue.Enqueue(uninstalled);

        var uninstallQueue = UninstallQueue.ToArray();
        UninstallQueue.Clear();

        foreach (var pkg in Settings.InstallPackages)
            InstallQueue.Enqueue(pkg);
        Settings.InstallPackages.Clear();

        var installQueue = InstallQueue.ToArray();
        InstallQueue.Clear();

        foreach (var id in uninstallQueue)
        {
            if (PreservedPackages.Contains(id)) continue;
            await Uninstall(id);
        }

        foreach (var id in installQueue)
        {
            try
            {
                await StagePackageId(id);
            }
            catch (Exception ex)
            {
                Log.LogError($"Unable to stage [{id}]", ex);
            }
        }

        var loosePackages = Directory.GetFiles(PackageFolder, "*.napa");
        foreach (var loosePackage in loosePackages)
        {
            try
            {
                await Install(loosePackage);
            }
            catch (Exception ex)
            {
                Log.LogError(string.Empty, ex);
            }
            File.Delete(loosePackage);
        }

        await foreach (var package in GetInstalledPackages())
        {
            result.Add(package);
        }

        Installed.SetItems(result);
        return result;
        //if (locked)
        //    UpdateLock.Release();
    }

    private async Task UpdateSourcePackages()
    {
        var packages = new List<SourcePackage>();
        var sources = Settings.Sources.ToArray();
        foreach (var src in sources)
        {
            var list = await GetPackagesFrom(src.Path);
            if (list is null) continue;

            foreach (var package in list)
            {
                var path = string.Empty;
                if (NapaLib.IsHttp(src.Path))
                {
                    var filename = Path.GetFileName(src.Path);
                    path = src.Path.Replace(filename, package.Path);
                }
                else
                {
                    path = NapaLib.PlatformPath(
                        Path.Join(
                            Path.GetDirectoryName(src.Path),
                            package.Path
                        )
                    );
                }

                packages.Add(new(package, src.Name, path));
            }
        }

        SourcePackages.Clear();
        SourcePackages.AddRange(packages);
    }
}