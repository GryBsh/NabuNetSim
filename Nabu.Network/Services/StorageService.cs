using Napa;
using YamlDotNet.Serialization;

namespace Nabu.Services
{
    public enum StorageUpdateType
    {
        None = 0,
        SymLink,
        Copy,
        Move,
        Mirror
    }

    public static class StorageNames
    {
        public const string SourceFolder = "Source";
    }

    public static class StorageOption
    {
        public static string UpdateType { get; } = nameof(UpdateType).ToLowerInvariant();
    }

    public record StorageOptions(string Path, string? Name, StorageUpdateType UpdateType);

    public class StorageService
    {
        private readonly SemaphoreSlim Lock = new SemaphoreSlim(1);
        private readonly ILog Logger;

        public StorageService(ILog<StorageService> console, Settings settings)
        {
            Logger = console;
            Settings = settings;
        }

        public Settings Settings { get; }
        private bool MigratedToIsolatedStorage { get; set; } = false;

        public void AttachStorage(AdaptorSettings settings, string name)
        {
            var root = new DirectoryInfo(Settings.StoragePath);
            if (!Path.Exists(root.FullName)) return;

            settings.StoragePath = Path.Combine(root.FullName, name);

            var source = SourceFolder(root.FullName);

            MigratedToIsolatedStorage = Path.Exists(source);
            if (!MigratedToIsolatedStorage)
            {
                Logger.WriteWarning("Migrating items from Storage root to File Source");
                UpdatePath(source, root.FullName, SearchOption.TopDirectoryOnly, StorageUpdateType.Move);
                MigratedToIsolatedStorage = true;
            }

            if (!Path.Exists(settings.StoragePath))
                Directory.CreateDirectory(settings.StoragePath);

            UpdateStoragePath(settings, source, SearchOption.AllDirectories);
        }

        public void UpdatePath(string destination, string source, SearchOption options, StorageUpdateType type, IList<StorageOptions>? special = null, string[]? excludePaths = null, bool force = false)
        {
            lock (Lock)
            {
                var sourceFolder = new DirectoryInfo(source);
                IEnumerable<FileInfo> files = sourceFolder.GetFiles("*", options);
                if (excludePaths is not null)
                {
                    bool notExcluded(FileInfo file)
                    {
                        foreach (var excluded in excludePaths)
                        {
                            if (file.FullName.StartsWith(excluded))
                                return false;
                        }
                        return true;
                    }
                    files = files.Where(notExcluded);
                }

                //files = files.Where(f => !f.Name.StartsWith('.'));
                var destinationName = Path.GetFileName(Path.TrimEndingDirectorySeparator(destination));
                if (special is not null)
                {
                    var relativeFolders = sourceFolder.GetDirectories("*", options).Select(f => Path.GetRelativePath(sourceFolder.FullName, f.FullName));
                    var specialFolders = relativeFolders.Where(f => special.Any(x => x.Path == f));
                    foreach (var folder in specialFolders)
                    {
                        var storageOptions = special.FirstOrDefault(o => o.Path == folder);
                        var updateType = storageOptions?.UpdateType;
                        if (updateType is StorageUpdateType.None)
                            continue;

                        var origin = Path.Join(sourceFolder.FullName, folder);
                        var target = Path.Join(destination, folder);

                        if (updateType is StorageUpdateType.Move)
                        {
                            Directory.Move(origin, target);
                            Logger.Write($"Moved: {folder} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.Copy)
                        {
                            var toCopy = Directory.GetFiles(origin, "*", options);
                            foreach (var c in toCopy)
                            {
                                var tPath = Path.GetRelativePath(origin, c);
                                var t = Path.Join(target, tPath);
                                File.Copy(c, t, true);
                            }
                            Logger.Write($"Copied: {folder} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.SymLink)
                        {
                            if (Directory.Exists(target))
                                Directory.Delete(target);

                            Directory.CreateSymbolicLink(origin, target);
                            Logger.Write($"SymLinked: {folder} to {destinationName}");
                        }
                    }
                }

                foreach (var file in files)
                {
                    if (NabuLib.IsSymLink(file.FullName))
                    {
                        if (!Path.Exists(NabuLib.ResolveLink(file.FullName)))
                        {
                            Logger.WriteWarning($"Deleting, missing target {file.Name}");
                            File.Delete(file.FullName);
                            continue;
                        }
                    }

                    var filePath = Path.GetRelativePath(sourceFolder.FullName, file.FullName);
                    var updateType = type switch
                    {
                        StorageUpdateType.Mirror =>
                            NabuLib.IsSymLink(file.FullName) ?
                                StorageUpdateType.SymLink :
                                StorageUpdateType.Copy,
                        _ => type
                    };

                    if (special is not null)
                    {
                        var storageOptions = special.FirstOrDefault(o => o.Path == filePath);
                        if (storageOptions is not null)
                        {
                            updateType = storageOptions.UpdateType is StorageUpdateType.None ?
                                         updateType :
                                         storageOptions.UpdateType;

                            filePath = string.IsNullOrWhiteSpace(storageOptions.Name) ?
                                       filePath :
                                       storageOptions.Name;
                        }
                    }

                    var newPath = Path.Combine(destination, filePath);
                    var newLastModified = Path.Exists(newPath) ?
                                            new FileInfo(newPath).LastWriteTime :
                                            DateTime.MinValue;

                    if (force || newLastModified < file.LastWriteTime)
                    {
                        var outDir = Path.GetDirectoryName(newPath);

                        if (!string.IsNullOrWhiteSpace(outDir) && !Path.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        if (updateType is StorageUpdateType.Move)
                        {
                            File.Move(file.FullName, newPath, true);
                            Logger.Write($"Moved: {filePath} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.Copy)
                        {
                            File.Copy(file.FullName, newPath, true);
                            Logger.Write($"Copied: {filePath} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.SymLink)
                        {
                            if (File.Exists(newPath))
                                File.Delete(newPath);

                            File.CreateSymbolicLink(newPath, file.FullName);
                            Logger.Write($"SymLinked: {filePath} to {destinationName}");
                        }
                    }
                }

                var badLinks = Directory.GetFiles(destination, "*", SearchOption.AllDirectories)
                                        .Where(NabuLib.IsSymLink)
                                        .Where(p => File.ResolveLinkTarget(p, true) is FileSystemInfo f && !f.Exists);
                foreach (var badLink in badLinks)
                {
                    Logger.WriteWarning($"Remove: Missing target: {badLink}");
                    File.Delete(badLink);
                }
            }
        }

        public void UpdateStorageFromPackages(IPackageManager packages)
        {
            if (!Path.Exists(Settings.LocalProgramPath))
                Directory.CreateDirectory(Settings.LocalProgramPath);

            var storage = SourceFolder(Settings.StoragePath);
            foreach (var package in packages.Installed)
            {
                var packageStorage = Path.Combine(package.Path, PackageFeatures.Storage);
                var hasStorage = Path.Exists(packageStorage);

                if (!hasStorage) continue;

                if (package.Storage is not null)
                {
                    var overrides = new List<StorageOptions>();
                    foreach (var item in package.Storage)
                    {
                        overrides.Add(
                            new(
                                NabuLib.PlatformPath(item.Path),
                                item.Name is not null ? NabuLib.PlatformPath(item.Name) : string.Empty,
                                item.Option<StorageUpdateType>(StorageOption.UpdateType)
                            )
                        );
                    }
                    UpdatePath(storage, packageStorage, SearchOption.AllDirectories, StorageUpdateType.SymLink, overrides);
                }
                else
                    UpdatePath(storage, packageStorage, SearchOption.AllDirectories, StorageUpdateType.SymLink);
            }
        }

        public void UpdateStoragePath(AdaptorSettings settings, string source, SearchOption options)
            => UpdatePath(settings.StoragePath, source, options, StorageUpdateType.Mirror);

        private string SourceFolder(string path)
        {
            return Path.Combine(path, StorageNames.SourceFolder);
        }
    }
}