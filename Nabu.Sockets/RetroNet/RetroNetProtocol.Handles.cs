using Nabu.Adaptor;
using Nabu.Network.RetroNetHandle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nabu.Network.RetroNet;

public partial class RetroNetProtocol : Protocol
{
    private async Task FileOpen(CancellationToken cancel)
    {
        var filename = RecvString();
        var flags = (FileOpenFlags)NabuLib.ToShort(Reader.ReadBytes(2));
        var handle = Reader.ReadByte();

        await FileOpen(filename, flags, handle, cancel);
    }

    async Task FileOpen(string filename, FileOpenFlags flags, byte handle, CancellationToken cancel)
    {
        if (handle is 0xFF) handle = NextIndex();
        Log($"Open: {handle} {filename}");

        var bytes = await FileHandler(filename).Get(filename, cancel);

        Slots[handle] = filename switch
        {
            _ when Http().IsMatch(filename) => new RetroNetMemoryHandle(Logger, Settings, bytes),
            _ when Memory().IsMatch(filename) => new RetroNetMemoryHandle(Logger, Settings, bytes),
            _ => new RetroNetFileHandle(Logger, Settings)
        };
        var opened = await Slots[handle].Open(filename, flags, cancel);
        if (opened is false) Writer.Write(0xFF);
        else Writer.Write(handle);
    }

    private async Task FileHandleClose(CancellationToken cancel)
    {
        var handle = Recv();
        await Slots[handle].Close(cancel);
        Log($"Close: {handle}");
        Slots.Remove(handle);
    }

    private async Task FileHandleRead(CancellationToken cancel)
    {
        var handle = Recv();
        var offset = RecvInt();
        var length = RecvShort();
        var bytes = await Slots[handle].Read(offset, length, cancel);
        Log($"Read: {handle}: O:{offset} L:{length}");
        WriteBuffer(bytes);
    }

    private async Task FileHandleSize(CancellationToken cancel)
    {
        var handle = Recv();
        var size = await Slots[handle].Size(cancel);
        Log($"Size: {handle}");
        Writer.Write(size);
    }

    private async Task FileHandleSeek(CancellationToken cancel)
    {
        var handle = Reader.ReadByte();
        var offset = RecvInt();
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
        var handle = Recv();
        var length = RecvShort();
        var bytes = await Slots[handle].ReadSequence(length, cancel);
        Log($"SeqRead: {handle}: O:{Slots[handle].Position} L:{length}");
        WriteBuffer(bytes);
        return;

    }

    private async Task FileHandleDetails(CancellationToken cancel)
    {
        var handle = Recv();
        var file = await Slots[handle].Details(cancel);
        Log($"Details: {handle}");

        var filename = file.Filename;

        var fnLength = filename.Length > 64 ? 64 : filename.Length;
        filename = filename[..fnLength].ToUpper();
        Writer.Write(file);
    }

    private async Task FileHandleTruncate(CancellationToken cancel)
    {
        var handle = Recv();
        await Slots[handle].Empty(cancel);
        Log($"Truncated: {handle}");
    }

    private async Task FileHandleDelete(CancellationToken cancel)
    {
        var handle = Recv();
        var offset = RecvInt();
        var length = RecvShort();
        Log($"Delete: {handle}: O:{offset}  L:{length}");
        await Slots[handle].Delete(offset, length, cancel);
    }

    private async Task FileHandleInsert(CancellationToken cancel)
    {
        var handle = Recv();
        var offset = RecvInt();
        var length = RecvShort();
        var data = Reader.ReadBytes(length);
        Log($"Insert: {handle}: O:{offset}  L:{length}");
        await Slots[handle].Insert(offset, data, cancel);
    }

    private async Task FileHandleAppend(CancellationToken cancel)
    {
        var handle = Recv();
        var length = RecvShort();
        var bytes = Reader.ReadBytes(length);
        Log($"Append: {handle}: L:{length}");
        await Slots[handle].Append(bytes, cancel);
    }

    private async Task FileHandleReplace(CancellationToken cancel)
    {
        var handle = Recv();
        var offset = RecvInt();
        var length = RecvShort();
        var data = Reader.ReadBytes(length);
        Log($"Replace: {handle}: O:{offset}  L:{length}");
        await Slots[handle].Replace(offset, data, cancel);
    }
}