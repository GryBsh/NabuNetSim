using Gry;

namespace NHACP.V01
{
    public static class NHACPMessages
    {
        public const string Magic = "ACP";
        public static ushort[] SupportedVersions = [0, 1];

        public static Memory<byte> Buffer(Memory<byte> buffer)
        {
            return Frame(
                0x84,
                Bytes.FromUShort((ushort)buffer.Length),
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
                NHACPStructures.DateTime(date).ToArray()
            );
        }

        public static Memory<byte> DirectoryEntry(string path, int maxNameLength)
        {
            return Frame(
                0x86,
                NHACPStructures.FileInfo(path, maxNameLength).ToArray()
            );
        }

        public static Memory<byte> Error(NHACPErrors code, string message)
        {
            return Frame(
                0x82,
                Bytes.FromUShort((ushort)code),
                Bytes.ToSizedASCII(message).ToArray()
            );
        }

        public static Memory<byte> Frame(byte type, params Memory<byte>[] message)
        {
            return Bytes.Frame(
                new[] { type },
                message
            );
        }

        public static Memory<byte> Int(uint number)
        {
            return Frame(
                0x88,
                Bytes.FromUInt(number)
            );
        }

        public static Memory<byte> NHACPStarted(byte sessionId, ushort version, string id)
        {
            return Frame(
                0x80,
                new byte[] { sessionId },
                Bytes.FromUShort(version),
                Bytes.ToSizedASCII(id).ToArray()
            );
        }

        public static Memory<byte> OK() => new byte[] { 0x81 };

        public static Memory<byte> Short(ushort shrt)
        {
            return Frame(
                0x88,
                Bytes.FromUShort(shrt)
            );
        }

        public static Memory<byte> StorageLoaded(byte index, int size)
        {
            return Frame(
                0x83,
                new[] { index },
                Bytes.FromInt(size)
            );
        }
    }
}