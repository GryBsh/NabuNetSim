using Nabu.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Nabu.Network.NHACP.V01;

public static class NHACPStructure
{
    public static Span<byte> DateTime(DateTime dateTime)
    {
        return NabuLib.Concat(
            NabuLib.FromASCII(dateTime.ToString("yyyyMMdd")).ToArray(),
            NabuLib.FromASCII(dateTime.ToString("HHmmss")).ToArray()
        ).ToArray()
        .AsSpan();
    }

    public static Span<byte> FileInfo(string path)
    {
        var flags = AttrFlags.None;
        
        DateTime lastWrite = System.DateTime.MinValue;
        int length = 0;
        if (File.Exists(path)) {
            var info = new FileInfo(path);
            lastWrite = info.LastWriteTime;
            flags |= AttrFlags.Read;
            if (info.IsReadOnly is false)
                flags |= AttrFlags.Write;
            if (info.Attributes.HasFlag(FileAttributes.Offline) ||
                info.Attributes.HasFlag(FileAttributes.IntegrityStream) ||
                info.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                info.Attributes.HasFlag(FileAttributes.Device)
            )   flags |= AttrFlags.Special;
        } 
        else if (Directory.Exists(path))
        {
            var info = new DirectoryInfo(path);
            lastWrite = info.LastWriteTime;
            flags |= AttrFlags.Directory;
        }
            

        return NabuLib.Concat(
            DateTime(lastWrite).ToArray(),
            NabuLib.FromShort((short)flags),
            NabuLib.FromInt(length)
        ).ToArray()
        .AsSpan();
    }

    public static string String(byte[] buffer)
    {
        var str = string.Empty;
        for (int j = 0; j < buffer.Length; j++)
        {
            if (buffer[j] is 0x00) break;
            str += (char)buffer[j];
        }
        return str;
    }

}
