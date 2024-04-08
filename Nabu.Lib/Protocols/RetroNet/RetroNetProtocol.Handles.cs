//using Nabu.Network.RetroNetHandle;

using Gry.Protocols;
using Nabu.Protocols.RetroNet;
using Nabu.Settings;

namespace Nabu.Protocols.RetroNet
{
    public partial class RetroNetProtocol : Protocol<AdaptorSettings>
    {
        private async Task FileOpen(CancellationToken cancel)
        {
            var filename = ReadString();
            var flags = (FileOpenFlags)NabuLib.ToUShort(Reader.ReadBytes(2));
            var handle = Reader.ReadByte();

            await FileOpen(filename, flags, handle, cancel);
        }

        private async Task FileOpen(string filename, FileOpenFlags flags, byte handle, CancellationToken cancel)
        {
            if (handle is 0xFF) handle = NextSlotIndex();
            Log($"Open: {handle} {filename}");
            var path = filename switch
            {
                _ when NabuLib.IsHttp(filename) => NabuLib.Uri(Adapter, filename),
                _ when Memory().IsMatch(filename) => NabuLib.Uri(Adapter, filename),
                _ => NabuLib.FilePath(Adapter, filename)
            };
            if (path != filename)
            {
                Log($"Redirect: {handle} {path}");
            }

            Slots[handle] = path switch
            {
                _ when NabuLib.IsHttp(path) => new RetroNetMemoryHandle(Logger, Adapter, Http?.CachePath(path), await FileHandler(path).Get(path, cancel)),
                _ when Memory().IsMatch(path) => new RetroNetMemoryHandle(Logger, Adapter),
                _ => new RetroNetFileHandle(Logger, Adapter)
            };

            var opened = await Slots[handle].Open(path, flags, cancel);
            if (opened is false) Writer.Write(0xFF);
            else Writer.Write(handle);
        }

        private async Task FileHandleClose(CancellationToken cancel)
        {
            var handle = Read();
            await Slots[handle].Close(cancel);
            Log($"Close:{handle}");
            Slots.Remove(handle);
        }

        private async Task FileHandleRead(CancellationToken cancel)
        {
            var handle = Read();
            var offset = ReadInt();
            var length = ReadShort();
            var bytes = await Slots[handle].Read(offset, length, cancel);
            Log($"Read:{handle}: O:{offset} L:{length}");
            WriteBuffer(bytes);
        }

        private async Task FileHandleSize(CancellationToken cancel)
        {
            var handle = Read();
            var size = await Slots[handle].Size(cancel);
            Log($"Size:{handle} {size}");
            Writer.Write(size);
        }

        private async Task FileHandleSeek(CancellationToken cancel)
        {
            var handle = Reader.ReadByte();
            var offset = ReadInt();
            var seekOption = (FileSeekFlags)Reader.ReadByte();
            Log($"Seek: {handle}: O:{offset} {Enum.GetName(seekOption)}");
            Writer.Write(
                NabuLib.FromInt(
                    await Slots[handle].Seek(offset, seekOption, cancel)
                )
            );
        }

        private async Task FileHandleReadSequence(CancellationToken cancel)
        {
            var handle = Read();
            var length = ReadShort();
            var bytes = await Slots[handle].ReadSequence(length, cancel);
            Log($"SeqRead:{handle}: O:{Slots[handle].Position} L:{length}");
            WriteBuffer(bytes);
            return;
        }

        private async Task FileHandleDetails(CancellationToken cancel)
        {
            var handle = Read();
            var file = await Slots[handle].Details(cancel);
            Log($"Details:{handle}");

            //var filename = file.Filename;

            //var fnLength = filename.Length > 64 ? 64 : filename.Length;
            //file.Filename = filename[..fnLength].ToUpper();
            Writer.Write(file);
        }

        private async Task FileHandleTruncate(CancellationToken cancel)
        {
            var handle = Read();
            await Slots[handle].Empty(cancel);
            Log($"Truncated:{handle}");
        }

        private async Task FileHandleDelete(CancellationToken cancel)
        {
            var handle = Read();
            var offset = ReadInt();
            var length = ReadShort();
            Log($"Delete:{handle}: O:{offset}  L:{length}");
            await Slots[handle].Delete(offset, length, cancel);
        }

        private async Task FileHandleInsert(CancellationToken cancel)
        {
            var handle = Read();
            var offset = ReadInt();
            var length = ReadShort();
            var data = Reader.ReadBytes(length);
            Log($"Insert:{handle}: O:{offset}  L:{length}");
            await Slots[handle].Insert(offset, data, cancel);
        }

        private async Task FileHandleAppend(CancellationToken cancel)
        {
            var handle = Read();
            var length = ReadShort();
            var bytes = Reader.ReadBytes(length);
            Log($"Append:{handle}: L:{length}");
            await Slots[handle].Append(bytes, cancel);
        }

        private async Task FileHandleReplace(CancellationToken cancel)
        {
            var handle = Read();
            var offset = ReadInt();
            var length = ReadShort();
            var data = Reader.ReadBytes(length);
            Log($"Replace:{handle}: O:{offset}  L:{length}");
            await Slots[handle].Replace(offset, data, cancel);
        }
    }
}