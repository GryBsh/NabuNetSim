using Microsoft.Extensions.Options;
using Napa;
using System.Runtime.InteropServices;
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
        private const string AdaptorFolderPrefix = "-";
        private const string COMPortName = "COM";
        private readonly SemaphoreSlim Lock = new SemaphoreSlim(1);
        private readonly ILog Logger;

        public StorageService(ILog<StorageService> console, Settings settings)
        {
            Logger = console;
            Settings = settings;
        }

        public Settings Settings { get; }

        private static bool IsAtLeastWin10Build14972 =>
                IsWindows &&
                (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 14972) ||
                Environment.OSVersion.Version.Major >= 11;

        private static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
        private bool MigratedToIsolatedStorage { get; set; } = false;

        public void AttachStorage(AdaptorSettings settings, string name)
        {
            var root = new DirectoryInfo(Settings.StoragePath);
            if (!Path.Exists(root.FullName))
            {
                root = Directory.CreateDirectory(Settings.StoragePath);
            }

            var source = SourceFolder(root.FullName);
            var sourceExists = Path.Exists(source);
            var foldersWithLegacyNames = root.GetDirectories().Where(d => d.FullName != source).Where(d => !d.Name.StartsWith(AdaptorFolderPrefix));
            MigratedToIsolatedStorage = sourceExists && !foldersWithLegacyNames.Any();
            if (!MigratedToIsolatedStorage)
            {
                Logger.Write("Migrating items from Storage root to File Source");
                if (!sourceExists) UpdatePath(source, root.FullName, SearchOption.TopDirectoryOnly, StorageUpdateType.Move);

                foreach (var folder in foldersWithLegacyNames)
                {
                    var newName = folder switch
                    {
                        _ when folder.Name.StartsWith(COMPortName) => folder.Name.Replace(COMPortName, AdaptorFolderName(COMPortName)),
                        _ => AdaptorFolderName(folder.Name)
                    };

                    var newPath = Path.Join(folder.Parent!.FullName, newName);
                    Logger.Write($"Migrating {folder} to {newPath}");
                    Directory.Move(folder.FullName, newPath);
                }
                MigratedToIsolatedStorage = true;
            }

            settings.StoragePath = Path.Combine(root.FullName, AdaptorFolderName(name));           

            if (!Path.Exists(settings.StoragePath))
                Directory.CreateDirectory(settings.StoragePath);

            CleanUpLinks(settings.StoragePath, SearchOption.AllDirectories);
            UpdateStoragePath(settings, source, SearchOption.AllDirectories);
        }

        void CleanUpLinks(string path, SearchOption options)
        {
            var files = new DirectoryInfo(path).GetFiles("*", options);
            foreach (var file in files)
            {
                if (NabuLib.IsSymLink(file.FullName))
                {
                    var target = NabuLib.ResolveLink(file.FullName);
                    if (!Path.Exists(target))
                    {
                        Logger.WriteWarning($"Deleting, missing target {file.Name}");
                        File.Delete(file.FullName);
                        continue;
                    }
                }
            }
        }

        public void UpdatePath(string destination, string source, SearchOption options, StorageUpdateType type, IList<StorageOptions>? special = null, string[]? excludePaths = null, bool force = false)
        {
            lock (Lock)
            {
                var sourceFolder = new DirectoryInfo(source);
                CleanUpLinks(source, options);
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

                    var filePath = Path.GetRelativePath(sourceFolder.FullName, file.FullName);
                    var isSymLink = NabuLib.IsSymLink(file.FullName);
                    var updateType = type switch
                    {
                        StorageUpdateType.Mirror or StorageUpdateType.SymLink when isSymLink && IsWindows && !IsAtLeastWin10Build14972 => StorageUpdateType.Copy,
                        StorageUpdateType.Mirror when isSymLink => StorageUpdateType.SymLink,
                        StorageUpdateType.Mirror => StorageUpdateType.Copy,
                        StorageUpdateType.SymLink when !IsAtLeastWin10Build14972 =>
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

        private static string AdaptorFolderName(string name) => $"{AdaptorFolderPrefix}.{name}";

        private string SourceFolder(string path)
        {
            return Path.Combine(path, StorageNames.SourceFolder);
        }
    }
}