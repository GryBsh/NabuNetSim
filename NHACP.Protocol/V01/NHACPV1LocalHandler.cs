using Gry;
using Gry.Adapters;
using Microsoft.Extensions.Logging;

namespace NHACP.V01
{
    public class NHACPV1LocalHandler(ILogger logger, AdapterDefinition settings, bool isSymLink, string originalUri) : INHACPStorageHandler
    {
        private readonly List<string> _list = [];
        private DirectoryInfo? _directory = new(settings.StoragePath);
        private FileInfo? FileInfo;
        private int ListPosition = 0;
        private FileStream? FileStream;
        private NHACPOpenFlags Flags;
        private string? Path;

        public uint Length => (uint?)FileInfo?.Length ?? 0;
        public uint Position { get; private set; }
        private bool Create => Flags.HasFlag(NHACPOpenFlags.Create);
        private bool Exclusive => Flags.HasFlag(NHACPOpenFlags.Exclusive);
        private bool Folder => Flags.HasFlag(NHACPOpenFlags.Directory);

        private bool ReadOnly => Flags.HasFlag(NHACPOpenFlags.ReadOnly);
        private bool ReadWrite => Flags.HasFlag(NHACPOpenFlags.ReadWrite);
        private bool ReadWriteProtect => Flags.HasFlag(NHACPOpenFlags.ReadWriteProtect);
        private bool Writable => ReadWrite || ReadWriteProtect;

        public Task Close()
        {
            FileInfo = null;
            _list.Clear();
            FileStream?.Dispose();
            return Task.CompletedTask;
        }

        public void End()
        {
            Close();
        }

        public async Task<(bool, string, Memory<byte>, NHACPErrors)> Get(uint offset, uint length, bool realLength = false)
        {
            if (FileStream is null)
                return (false, string.Empty, Array.Empty<byte>(), NHACPErrors.InvalidRequest);

            if (offset > FileStream.Length)
                return (true, string.Empty, Array.Empty<byte>(), 0);

            var buffer = new Memory<byte>(new byte[length]);

            logger.LogDebug($"Reading {length} bytes from {offset}, File Length: {FileStream.Length}");
            FileStream.Seek(offset, SeekOrigin.Begin);
            var read = await FileStream.ReadAsync(buffer);
            if (read != length && realLength)
                buffer = buffer[0..read];

            return (true, string.Empty, buffer, 0);
        }

        public (bool, string?, string, NHACPErrors) GetDirEntry(byte maxNameLength)
        {
            if (ListPosition == _list.Count)
                return (true, null, string.Empty, 0);
            var entry = _list[ListPosition++];
            return (true, entry, string.Empty, 0);
        }

        public (bool, string, string, NHACPErrors) Info()
        {
            if (Path is null) return (false, string.Empty, "No file/directory open", NHACPErrors.InvalidRequest);
            return (true, Path, string.Empty, 0);
        }

        public (bool, string, NHACPErrors) ListDir(string pattern)
        {
            if (_directory is null) return (false, string.Empty, NHACPErrors.InvalidRequest);

            pattern = string.IsNullOrWhiteSpace(pattern) ? "*" : pattern;
            _list.Clear();
            ListPosition = 0;

            var r = Directories.List(_directory.FullName, pattern);

            _list.AddRange(r);
            return (true, string.Empty, 0);
        }

        public Task<(bool, string, uint, NHACPErrors)> Open(NHACPOpenFlags flags, string uri)
        {
            Flags = flags;
            Path = uri;

            var exists = File.Exists(Path);
            if (Exclusive && exists)
            {
                return Task.FromResult((false, "Exists", Length, NHACPErrors.Exists));
            }
            else if (!Folder && !exists && !Create)
            {
                return Task.FromResult((false, "Not found", Length, NHACPErrors.NotFound));
            }

            //Logger.Write($"Create: {create}, ReadWrite: {readWrite}");
            if (Folder is false && (exists || Create))
            {
                FileInfo = new FileInfo(Path);
                if (ReadWrite && FileInfo.IsReadOnly)
                {
                    return Task.FromResult((false, "Read Only", Length, NHACPErrors.AccessDenied));
                }

                FileStream = new FileStream(
                    FileInfo!.FullName,
                    Create ? FileMode.OpenOrCreate : FileMode.Open,
                    Writable ? FileAccess.ReadWrite : FileAccess.Read,
                    FileShare.ReadWrite
                );

                FileInfo = new FileInfo(Path);
                if (!FileInfo.Exists)
                {
                    return Task.FromResult((false, "Cant Open", (uint)0, NHACPErrors.NotPermitted));
                }
                //Length = (int)_file.Length;
                return Task.FromResult((true, string.Empty, Length, NHACPErrors.Undefined));
            }
            else if (Folder && Directory.Exists(Path))
            {
                _directory = new DirectoryInfo(Path);
                return Task.FromResult((true, string.Empty, (uint)0, NHACPErrors.Undefined));
            }
            return Task.FromResult((false, "Not found", (uint)0, NHACPErrors.NotFound));
        }

        public async Task<(bool, string, NHACPErrors)> Put(uint offset, Memory<byte> buffer)
        {
            if (FileStream is null)
                return (false, string.Empty, NHACPErrors.InvalidRequest);

            if (ReadWriteProtect && FileInfo?.IsReadOnly is true)
                return (false, "Write Protected", NHACPErrors.WriteProtected);
            else if (ReadOnly && !ReadWriteProtect && FileInfo?.IsReadOnly is true)
                return (false, "Read Only", NHACPErrors.NotPermitted);

            if (isSymLink && settings.EnableCopyOnSymLinkWrite)
            {
                if (Path is null || FileStream is null)
                {
                    return (false, "Not Open", NHACPErrors.NotPermitted);
                }
                FileStream.Close();
                FileStream.Dispose();
                logger.LogInformation($"Copying SymLink target to `{originalUri}`");
                File.Copy(Path, originalUri, true);

                Path = originalUri;
                isSymLink = false;

                FileInfo = new FileInfo(Path);
                FileStream = new FileStream(
                    FileInfo!.FullName,
                    FileMode.Open,
                    Writable ? FileAccess.ReadWrite : FileAccess.Read,
                    FileShare.ReadWrite
                );
                //length = (int)_file.Length;
            }
            else if (isSymLink)
            {
                logger.LogWarning($"SymLinks are write-protected: `{Path}`");
                return (false, "SymLink", NHACPErrors.WriteProtected);
            }

            FileStream!.Seek(offset, SeekOrigin.Begin);
            await FileStream!.WriteAsync(buffer);
            return (true, string.Empty, 0);
        }

        public async Task<(bool, string, Memory<byte>, NHACPErrors)> Read(uint length)
        {
            if (Position > Length) return (true, string.Empty, Array.Empty<byte>(), NHACPErrors.Undefined);
            if (Position + length > Length) length = Length - Position;

            var result = await Get(Position, length, true);
            Position += length;
            return result;
        }

        public (bool, uint, string, NHACPErrors) Seek(uint offset, NHACPSeekOrigin origin)
        {
            if (FileStream is null)
                return (false, 0, string.Empty, NHACPErrors.InvalidRequest);

            uint? newPosition = origin switch
            {
                NHACPSeekOrigin.Set => offset,
                NHACPSeekOrigin.Current => Position + offset,
                NHACPSeekOrigin.End => Length - offset,
                _ => null
            };

            if (newPosition is null) return (false, Position, "Unknown Seek Origin", NHACPErrors.InvalidRequest);

            FileStream.Seek((long)newPosition!, SeekOrigin.Begin);

            return (true, Position, string.Empty, NHACPErrors.Undefined);
        }

        public (bool, uint, string, NHACPErrors) SetSize(uint size)
        {
            FileStream?.SetLength(size);
            return (true, (uint)(FileStream?.Length ?? 0), string.Empty, 0);
        }

        public async Task<(bool, string, NHACPErrors)> Write(Memory<byte> buffer)
        {
            if (Position > Length)
            {
                int fillLength = (int)(Position - Length);
                await Put(Length, Enumerable.Repeat((byte)0x00, fillLength).ToArray());
            }
            var result = await Put(Position, buffer);

            return result;
        }
    }
}