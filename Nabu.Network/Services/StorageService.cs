using Napa;
using System.Collections.Generic;
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

    public static class StorageOption
    {
        public static string UpdateType { get; } = nameof(UpdateType).ToLowerInvariant();
    }

    public record StorageOptions(string Path, string Name, StorageUpdateType UpdateType);

    public class StorageService
    {


        readonly ILog Logger;
        public StorageService(ILog<StorageService> console)
        {
            Logger = console;
        }

        readonly SemaphoreSlim Lock = new SemaphoreSlim(1);
        bool MigratedToIsolatedStorage { get; set; } = false;

        string SourceFolder(string path)
        {
            return Path.Combine(path, "Source");
        }

        public void UpdateStorageFromPackages(IPackageManager packages, AdaptorSettings settings)
        {
            var storage = SourceFolder(settings.StoragePath);
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
                                NabuLib.PlatformPath(item.Name),
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

        public void UpdatePath(string destination, string source, SearchOption options, StorageUpdateType type, IList<StorageOptions>? special = null, string[]? excludePaths = null, bool force = false)
        {
            var sourceFolder = new DirectoryInfo(source);
            IEnumerable<FileInfo> files = sourceFolder.GetFiles("*", options);
            if (excludePaths is not null)
            {
                bool notExcluded(FileInfo file)
                {
                    foreach (var excluded in excludePaths)
                    {
                        if (file.FullName.Contains(excluded))
                            return false;
                    }
                    return true;
                }
                files = files.Where(notExcluded);
            }
            var destinationName = Path.GetFileName(Path.TrimEndingDirectorySeparator(destination));

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


        }

        public void UpdateStorage(AdaptorSettings settings, string name)
        {
            var root = new DirectoryInfo(settings.StoragePath);
            if (!Path.Exists(root.FullName)) return;

            settings.StoragePath = Path.Combine(settings.StoragePath, name); ;

            var source = SourceFolder(root.FullName);

            lock (Lock)
            {
                MigratedToIsolatedStorage = Path.Exists(source);
                if (!MigratedToIsolatedStorage)
                {
                    Logger.WriteWarning("Migrating Classic Storage to Managed Storage");
                    UpdatePath(source, root.FullName, SearchOption.TopDirectoryOnly, StorageUpdateType.Move);
                    MigratedToIsolatedStorage = true;
                }
            }

            if (!Path.Exists(settings.StoragePath))
                Directory.CreateDirectory(settings.StoragePath);

            UpdateStoragePath(settings, source, SearchOption.AllDirectories);

        }
    }
}
