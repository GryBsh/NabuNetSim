﻿namespace Nabu.Network.NHACP.V01;

public static class NHACPMessage
{
    public const string Magic = "ACP";
    public static ushort[] SupportedVersions = new ushort[] { 0, 1 };

    public static Memory<byte> Buffer(Memory<byte> buffer)
    {
        return Frame(
            0x84,
            NabuLib.FromUShort((ushort)buffer.Length),
            buffer.ToArray()
        );
    }

    public static Memory<byte> Byte(byte byt)
    {
        return Frame(
            0x87,
            new[] { byt }
        );
    }

    public static Memory<byte> DateTime(DateTime date)
    {
        return Frame(
            0x85,
            NHACPStructure.DateTime(date).ToArray()
        );
    }

    public static Memory<byte> DirectoryEntry(string path, int maxNameLength)
    {
        return Frame(
            0x86,
            NHACPStructure.FileInfo(path, maxNameLength).ToArray()
        );
    }

    public static Memory<byte> Error(NHACPError code, string message)
    {
        return Frame(
            0x82,
            NabuLib.FromUShort((ushort)code),
            NabuLib.ToSizedASCII(message).ToArray()
        );
    }

    public static Memory<byte> Frame(byte type, params Memory<byte>[] message)
    {
        return NabuLib.Frame(
            new[] { type },
            message
        );
    }

    public static Memory<byte> Int(int number)
    {
        return Frame(
            0x88,
            NabuLib.FromInt(number)
        );
    }

    public static Memory<byte> NHACPStarted(byte sessionId, ushort version, string id)
    {
        return Frame(
            0x80,
            new byte[] { sessionId },
            NabuLib.FromUShort(version),
            NabuLib.ToSizedASCII("NONE").ToArray()
        );
    }

    public static Memory<byte> OK() => new byte[] { 0x81 };

    public static Memory<byte> Short(ushort shrt)
    {
        return Frame(
            0x88,
            NabuLib.FromUShort(shrt)
        );
    }

    public static Memory<byte> StorageLoaded(byte index, int size)
    {
        return Frame(
            0x83,
            new[] { index },
            NabuLib.FromInt(size)
        );
    }
}