﻿using Nabu.Services;

namespace Nabu.Adaptor
{
    public class StorageService
    {
        readonly IConsole Logger;
        public StorageService(IConsole<StorageService> console) { 
            Logger = console;
        }

        readonly SemaphoreSlim Lock = new SemaphoreSlim(1);

        public void InitializeStorage(AdaptorSettings settings, string name)
        {
            var root = new DirectoryInfo(settings.StoragePath);
            if (!Path.Exists(root.FullName)) return;

            settings.StoragePath = Path.Combine(settings.StoragePath, name);
            
            var sourceFolder = new DirectoryInfo(Path.Combine(root.FullName, "Source"));

            lock (Lock)
            {
                if (!sourceFolder.Exists)
                    sourceFolder.Create();

                var legacyFiles = root.GetFiles();
                foreach (var movable in legacyFiles)
                {
                    var newPath = Path.Combine(sourceFolder.FullName, movable.Name);
                    var newLastModified = Path.Exists(newPath) ?
                                            new FileInfo(newPath).LastWriteTime :
                                            DateTime.MinValue;

                    if (newLastModified < movable.LastWriteTime)
                    {
                        Logger.Write($"Migrating {movable.Name} from root to `Source` folder");
                        movable.MoveTo(Path.Combine(sourceFolder.FullName, movable.Name));
                    }
                }
            }

            if (!Path.Exists(settings.StoragePath))
                Directory.CreateDirectory(settings.StoragePath);

            var files = sourceFolder.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var filePath = Path.GetRelativePath(sourceFolder.FullName, file.FullName);
                var newPath = Path.Combine(settings.StoragePath, filePath);
                var newLastModified = Path.Exists(newPath) ?
                                        new FileInfo(newPath).LastWriteTime :
                                        DateTime.MinValue;

                if (newLastModified < file.LastWriteTime)
                {
                    var outDir = Path.GetDirectoryName(newPath);
                    Logger.Write($"Updating new or modified file {filePath}");
                    if (outDir is not null && !Path.Exists(outDir))
                        Directory.CreateDirectory(outDir);
                    File.Copy(file.FullName, newPath, true);
                }
            }

        }
    }
}
