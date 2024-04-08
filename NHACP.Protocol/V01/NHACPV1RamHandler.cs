
using Gry;
using Gry.Adapters;
using Microsoft.Extensions.Logging;

namespace NHACP.V01
{
    public class NHACPV1RamHandler(ILogger logger, AdapterDefinition settings) : INHACPStorageHandler
    {
        protected ILogger Logger = logger;
        protected AdapterDefinition Adaptor = settings;
        protected Memory<byte> Buffer = new();

        public uint Position { get; set; } = 0;
        public string Path { get; set; } = string.Empty;

        public virtual Task<(bool, string, uint, NHACPErrors)> Open(NHACPOpenFlags flags, string uri)
        {
            try
            {
                Path = uri;
                uri = uri.Replace("0x", string.Empty);
                var size = string.IsNullOrEmpty(uri) ? ushort.MaxValue : ushort.Parse(uri);
                Buffer = new byte[size];
                return Task.FromResult((true, string.Empty, (uint)Buffer.Length, NHACPErrors.Undefined));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, ex.Message, (uint)0, NHACPErrors.Undefined));
            }
        }

        public Task<(bool, string, Memory<byte>, NHACPErrors)> Get(uint offset, uint length, bool realLength = false)
        {
            try
            {
                if (offset >= Buffer.Length) return Task.FromResult((false, "Offset beyond end of file", (Memory<byte>)Array.Empty<byte>(), NHACPErrors.InvalidRequest));
                var (_, buffer) = Bytes.Slice(Buffer, (int)offset, (int)length);
                if (realLength is false && buffer.Length != length)
                {
                    var read = buffer;
                    buffer = new Memory<byte>(new byte[length]);
                    read.CopyTo(buffer[..read.Length]);
                }
                return Task.FromResult((true, string.Empty, buffer, NHACPErrors.Undefined));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, ex.Message, (Memory<byte>)Array.Empty<byte>(), NHACPErrors.Undefined));
            }
        }

        public virtual Task<(bool, string, NHACPErrors)> Put(uint offset, Memory<byte> buffer)
        {
            try
            {
                var length = buffer.Length + offset;
                if (length > Buffer.Length)
                {
                    var temp = new Memory<byte>(new byte[length]);
                    Buffer.CopyTo(temp);
                    Buffer = temp;
                }
                buffer.CopyTo(Buffer[(int)offset..]);
                return Task.FromResult((true, string.Empty, NHACPErrors.Undefined));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, ex.Message, NHACPErrors.Undefined));
            }
        }

        public void End()
        {
            Buffer = Array.Empty<byte>();
        }

        public (bool, uint, string, NHACPErrors) Seek(uint offset, NHACPSeekOrigin origin)
        {
            return (true, (uint)(Position += offset), string.Empty, NHACPErrors.Undefined);
        }

        public (bool, string, string, NHACPErrors) Info()
        {
            if (Path == string.Empty) return (false, string.Empty, "Memory Bank does not exist", NHACPErrors.InvalidRequest);
            return (true, Path, string.Empty, 0);
        }

        public (bool, uint, string, NHACPErrors) SetSize(uint size)
        {
            var tmpBuffer = new Memory<byte>(new byte[size]);
            Buffer.CopyTo(tmpBuffer);
            Buffer = tmpBuffer;
            return (true, (uint)size, string.Empty, 0);
        }

        public Task<(bool, string, Memory<byte>, NHACPErrors)> Read(uint length)
        {
            var (_, slice) = Bytes.Slice(Buffer, (int)Position, (int)length);
            Position += length;
            return Task.FromResult((true, string.Empty, slice, NHACPErrors.Undefined));
        }

        public Task<(bool, string, NHACPErrors)> Write(Memory<byte> buffer)
        {
            var length = buffer.Length + Position;
            if (length > Buffer.Length)
            {
                var temp = new Memory<byte>(new byte[length]);
                Buffer.CopyTo(temp);
                Buffer = temp;
            }
            buffer.CopyTo(Buffer[(int)Position..]);
            return Task.FromResult((true, string.Empty, NHACPErrors.Undefined));
        }

        public (bool, string, NHACPErrors) ListDir(string pattern)
        {
            throw new NotImplementedException();
        }

        public (bool, string?, string, NHACPErrors) GetDirEntry(byte maxNameLength)
        {
            throw new NotImplementedException();
        }

        public (bool, string, NHACPErrors) Remove(RemoveFlags removeFlags, string url)
        {
            throw new NotImplementedException();
        }

        public (bool, string, NHACPErrors) Rename(string oldName, string newName)
        {
            throw new NotImplementedException();
        }

        public Task Close()
        {
            End();
            return Task.CompletedTask;
        }
    }
}