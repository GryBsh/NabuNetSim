using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Nabu.Logs;
using Nabu.Settings;
using Nabu.Sources;
using Napa;
using System.IO;

namespace Nabu.Network
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

    public class StorageService(ILogger<StorageService> console, GlobalSettings settings, ISourceService sources)
    {
        public const string AdaptorFolderNamePrefix = $"{AdaptorFolderPrefix}.";
        private const string AdaptorFolderPrefix = "Client";
        private const string COMPortName = "COM";
        private const string OldAdaptorFolderPrefix = "-";
        private readonly SemaphoreSlim Lock = new(1);
        private readonly ILogger Logger = console;

        public GlobalSettings Settings { get; } = settings;
        public ISourceService Sources { get; } = sources;

        private static bool IsAtLeastWin10Build14972 =>
                IsWindows &&
                Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 14972 ||
                Environment.OSVersion.Version.Major >= 11;

        private static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
        private bool MigratedToIsolatedStorage { get; set; } = false;

        public string StorageRoot 
            =>  Path.IsPathRooted(Settings.StoragePath) ||
                Path.IsPathFullyQualified(Settings.StoragePath) ?
                    Settings.StoragePath :
                    Path.Join(AppContext.BaseDirectory, Settings.StoragePath);
        
        public string[] ListDirectories(string? path = null)
        {
            path = Path.Combine(StorageRoot, path ?? string.Empty);
            if (Path.Exists(path))
                return Directory.GetDirectories(path);
            return [];
        }

        public void AttachStorage(AdaptorSettings settings, string name)
        {
            var root = new DirectoryInfo(StorageRoot);
            if (!Path.Exists(root.FullName))
            {
                root = Directory.CreateDirectory(StorageRoot);
            }

            var source = SourceFolder(root.FullName);
            var sourceExists = Path.Exists(source);
            var foldersWithLegacyNames = root.GetDirectories().Where(d => d.FullName != source).Where(d => !d.Name.StartsWith(AdaptorFolderPrefix));
            MigratedToIsolatedStorage = sourceExists && !foldersWithLegacyNames.Any();
            if (!MigratedToIsolatedStorage)
            {
                MigrateToIsolatedStorage(root, source, sourceExists, foldersWithLegacyNames);
            }

            settings.StoragePath = Path.Combine(root.FullName, AdaptorFolderName(name));


            if (!Path.Exists(settings.StoragePath))
                Directory.CreateDirectory(settings.StoragePath);

            CleanUpLinks(settings.StoragePath, SearchOption.AllDirectories);
            UpdateStoragePath(settings, source, SearchOption.AllDirectories);
            //});
        }

        private void MigrateToIsolatedStorage(DirectoryInfo root, string source, bool sourceExists, IEnumerable<DirectoryInfo> folders)
        {
            Logger.LogInformation("Migrating items from Storage root to File Source");
            if (!sourceExists) UpdatePath(source, root.FullName, SearchOption.TopDirectoryOnly, StorageUpdateType.Move);

            foreach (var folder in folders)
            {
                var newName = folder.Name switch
                {
                    string n when n.StartsWith(COMPortName) => n.Replace(COMPortName, AdaptorFolderName(COMPortName)),
                    string n when n.StartsWith(OldAdaptorFolderPrefix) => n.Replace(OldAdaptorFolderPrefix, AdaptorFolderPrefix),
                    _ => AdaptorFolderName(folder.Name)
                };

                var newPath = Path.Join(folder.Parent!.FullName, newName);
                Logger.LogInformation($"Migrating {folder} to {newPath}");
                Directory.Move(folder.FullName, newPath);
            }
            MigratedToIsolatedStorage = true;
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
                CleanUpLinks(destination, SearchOption.AllDirectories);

                //files = files.Where(f => !f.Name.StartsWith('.'));
                var destinationName = Path.GetFileName(Path.TrimEndingDirectorySeparator(destination));
                if (special is not null)
                {
                    var relativeFolders = sourceFolder.GetDirectories("*", options).Select(f => Path.GetRelativePath(sourceFolder.FullName, f.FullName));
                    var specialFolders = relativeFolders.Where(f => special.Any(x => x.Path == f));
                    foreach (var folder in specialFolders)
                    {
                        var storageOptions = special.FirstOrDefault(o => o.Path == folder);
                        var updateType = storageOptions?.UpdateType is StorageUpdateType.SymLink && !Settings.EnableSymLinks ?
                                            StorageUpdateType.Copy :
                                            storageOptions?.UpdateType;

                        if (updateType is StorageUpdateType.None)
                            continue;

                        var origin = Path.Join(sourceFolder.FullName, folder);
                        var target = Path.Join(destination, folder);

                        if (updateType is StorageUpdateType.Move)
                        {
                            Directory.Move(origin, target);
                            Logger.LogInformation($"Moved: {folder} to {destinationName}");
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
                            Logger.LogInformation($"Copied: {folder} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.SymLink)
                        {
                            if (Directory.Exists(target))
                                Directory.Delete(target);

                            Directory.CreateSymbolicLink(origin, target);
                            Logger.LogInformation($"SymLinked: {folder} to {destinationName}");
                        }
                    }
                }

                foreach (var file in files)
                {
                    var filePath = Path.GetRelativePath(sourceFolder.FullName, file.FullName);
                    var sourceIsLink = NabuLib.IsSymLink(file.FullName);
                    StorageUpdateType updateType = StorageUpdateType.None;
                    updateType = type switch
                    {
                        StorageUpdateType.Mirror when !Settings.EnableSymLinks || IsWindows && !IsAtLeastWin10Build14972 => StorageUpdateType.Copy,
                        StorageUpdateType.Mirror when Settings.EnableSymLinks && sourceIsLink => StorageUpdateType.SymLink,
                        StorageUpdateType.Mirror => StorageUpdateType.Copy,
                        StorageUpdateType.SymLink when !Settings.EnableSymLinks || IsWindows && !IsAtLeastWin10Build14972 => StorageUpdateType.Copy,
                        _ => type
                    };

                    if (special is not null)
                    {
                        bool match(string pattern, string path)
                        {
                            Matcher matcher = new();
                            matcher.AddInclude(pattern);
                            return matcher.Match(path).HasMatches;
                        }

                        //var storageOptions = special.FirstOrDefault(o => o.Path == filePath);
                        var storageOptions = special.FirstOrDefault(o => match(o.Path, filePath));
                        if (storageOptions is not null)
                        {
                            updateType = storageOptions.UpdateType is StorageUpdateType.SymLink && !Settings.EnableSymLinks ?
                                            StorageUpdateType.Copy :
                                            storageOptions.UpdateType;

                            filePath = string.IsNullOrWhiteSpace(storageOptions.Name) ?
                                       filePath :
                                       storageOptions.Name;
                        }
                    }

                    var newPath = Path.Combine(destination, filePath);
                    var newPathExists = Path.Exists(newPath);
                    var newIsLink = newPathExists && NabuLib.IsSymLink(newPath);
                    var newLastModified = Path.Exists(newPath) ?
                                            new FileInfo(newPath).LastWriteTime :
                                            DateTime.MinValue;

                    var shouldForce = !newIsLink && updateType is StorageUpdateType.SymLink ||
                                      newIsLink && updateType is StorageUpdateType.Copy ||
                                      newIsLink && updateType is StorageUpdateType.Move;

                    if (force || shouldForce || file.LastWriteTime > newLastModified)
                    {
                        if (newPathExists)
                            File.Delete(newPath);

                        var outDir = Path.GetDirectoryName(newPath);

                        if (!string.IsNullOrWhiteSpace(outDir) && !Path.Exists(outDir))
                            Directory.CreateDirectory(outDir);

                        if (updateType is StorageUpdateType.Move)
                        {
                            File.Move(file.FullName, newPath, true);
                            Logger.LogInformation($"Moved: {filePath} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.Copy)
                        {
                            File.Copy(file.FullName, newPath, true);
                            Logger.LogInformation($"Copied: {filePath} to {destinationName}");
                        }
                        else if (updateType is StorageUpdateType.SymLink)
                        {
                            File.CreateSymbolicLink(newPath, file.FullName);
                            Logger.LogInformation($"SymLinked: {filePath} to {destinationName}");
                        }
                    }
                }
            }
        }

        public void UpdateStorageFromPackages(IPackageManager packages)
        {
            var path =  Path.IsPathRooted(Settings.LocalProgramPath) ||
                        Path.IsPathFullyQualified(Settings.LocalProgramPath) ?
                            Settings.LocalProgramPath :
                            Path.Join(AppContext.BaseDirectory, Settings.LocalProgramPath);

            if (!Path.Exists(path))
                Directory.CreateDirectory(path);

            var storage = SourceFolder(StorageRoot);
                if (!Path.Exists(storage))
                    Directory.CreateDirectory(storage);


            foreach (var package in packages.Installed.ToArray())
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
            //});
        }

        public void UpdateStoragePath(AdaptorSettings settings, string source, SearchOption options)
            => UpdatePath(settings.StoragePath, source, options, StorageUpdateType.Mirror);

        private static string AdaptorFolderName(string name) => $"{AdaptorFolderNamePrefix}{name}";

        private void CleanUpLinks(string path, SearchOption options)
        {
            if (!Directory.Exists(path)) return;
            var files = new DirectoryInfo(path).GetFiles("*", options);
            foreach (var file in files)
            {
                if (NabuLib.IsSymLink(file.FullName))
                {
                    var target = NabuLib.ResolveLink(file.FullName);
                    if (!Path.Exists(target))
                    {
                        Logger.LogWarning($"Deleting link with missing target: {file.Name}");
                        File.Delete(file.FullName);
                        continue;
                    }
                }
            }
        }

        private string SourceFolder(string path)
        {
            return Path.Combine(path, StorageNames.SourceFolder);
        }
    }
}