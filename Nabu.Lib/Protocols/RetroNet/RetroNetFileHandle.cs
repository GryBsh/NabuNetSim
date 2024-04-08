using Microsoft.Extensions.Logging;
using Nabu.Settings;
namespace Nabu.Protocols.RetroNet
{
    public class RetroNetFileHandle : NabuService, IRetroNetFileHandle
    {
        public RetroNetFileHandle(ILogger logger, AdaptorSettings settings) : base(logger, settings)
        {
        }

        public int Position { get; protected set; } = 0;

        //public FileInfo? FileHandle { get; set; }
        private string Filename { get; set; } = string.Empty;

        private FileOpenFlags? Flags { get; set; }

        public async Task Append(Memory<byte> data, CancellationToken cancel)
        {
            using var stream = Stream();
            await stream.WriteAsync(NabuLib.Append(Content(), data).ToArray(), cancel);
        }

        public Task Close(CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public async Task Delete(int offset, ushort length, CancellationToken cancel)
        {
            using var stream = Stream();
            await stream.WriteAsync(NabuLib.Delete(Content(), offset, length).ToArray());
        }

        public Task<FileDetails> Details(CancellationToken cancel)
        {
            return Task.FromResult(
                new FileDetails
                {
                    Created = File.GetCreationTime(Filename),
                    Modified = File.GetLastWriteTime(Filename),
                    Filename = Path.GetFileName(Filename),
                    FileSize = NabuLib.FileSize(Filename),
                }
            );
        }

        public Task Empty(CancellationToken cancel)
        {
            using var _ = Stream(FileMode.Truncate);
            return Task.CompletedTask;
        }

        public Task Insert(int offset, Memory<byte> data, CancellationToken cancel)
        {
            using var stream = Stream();
            stream.Write(NabuLib.Insert(Content(), offset, data).ToArray());
            return Task.CompletedTask;
        }

        public Task<bool> Open(string filename, FileOpenFlags flags, CancellationToken cancel)
        {
            Flags = flags;
            Filename = filename;
            return Task.FromResult(true);
        }

        public async Task<Memory<byte>> Read(int offset, ushort readLength, CancellationToken cancel)
        {
            var bytes = new Memory<byte>(new byte[readLength]);
            using var reader = Stream();
            reader.Seek(offset, SeekOrigin.Begin);
            await reader.ReadAsync(bytes, cancel);
            return bytes;
        }

        public async Task<Memory<byte>> ReadSequence(ushort readLength, CancellationToken cancel)
        {
            var end = Position + readLength;
            var length = new FileInfo(Filename).Length;
            if (end > length)
            {
                end = (int)length;
                readLength = (ushort)(length - Position);
            }
            if (Position >= length)
            {
                return Array.Empty<byte>();
            }
            //Log($"ReadSeq: S:{Position}, L:{readLength}, E:{end}");
            var bytes = await Read(Position, readLength, cancel);
            Position += readLength;
            return bytes;
        }

        public Task Replace(int offset, Memory<byte> data, CancellationToken cancel)
        {
            using var stream = Stream();
            stream.Write(NabuLib.Replace(Content(), offset, data).ToArray());
            return Task.CompletedTask;
        }

        public Task<int> Seek(int offset, FileSeekFlags flags, CancellationToken cancel)
        {
            Position = flags switch
            {
                FileSeekFlags.FromCurrent => Position + offset,
                FileSeekFlags.FromBeginning => offset,
                FileSeekFlags.FromEnd => (int)new FileInfo(Filename).Length - offset,
                _ => offset
            };
            return Task.FromResult(Position);
        }

        public Task<int> Size(CancellationToken cancel)
        {
            return Task.FromResult((int)new FileInfo(Filename).Length);
        }

        private Memory<byte> Content()
        {
            using var stream = Stream();
            using var reader = new BinaryReader(stream);

            return reader.ReadBytes((int)new FileInfo(Filename)!.Length);
        }

        private FileStream Stream(FileMode mode = FileMode.OpenOrCreate)
        {
            var stream = new FileStream(
                Filename,
                mode,
                Flags is FileOpenFlags.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.ReadWrite
            );
            return stream;
        }
    }
}