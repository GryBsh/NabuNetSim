using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gry
{
    public static partial class Bytes
    {
        /// <summary>
        ///     Converts  a String
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static Memory<byte> FromASCII(string buffer)
            => new ASCIIEncoding().GetBytes(buffer);

        /// <summary>
        ///     Converts a Boolean into a byte
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte FromBool(bool value) => (byte)(value ? 0x01 : 0x00);

        /// <summary>
        ///     Converts an Int to a little-endian
        ///     32 bit integer in bytes
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static byte[] FromInt(int number)
        {
            var buffer = new byte[4];
            buffer[0] = (byte)(number >> 0 & 0xFF);
            buffer[1] = (byte)(number >> 8 & 0xFF);
            buffer[2] = (byte)(number >> 16 & 0xFF);
            buffer[3] = (byte)(number >> 24 & 0xFF);
            return buffer;
        }
        public static byte[] FromUInt(uint number)
        {
            var buffer = new byte[4];
            buffer[0] = (byte)(number >> 0 & 0xFF);
            buffer[1] = (byte)(number >> 8 & 0xFF);
            buffer[2] = (byte)(number >> 16 & 0xFF);
            buffer[3] = (byte)(number >> 24 & 0xFF);
            return buffer;
        }

        /// <summary>
        ///     Converts an Short to a little-endian
        ///     16 bit integer in bytes
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static byte[] FromUShort(ushort number)
        {
            var buffer = new byte[2];
            buffer[0] = (byte)(number >> 0 & 0xFF);
            buffer[1] = (byte)(number >> 8 & 0xFF);
            return buffer;
        }

        public static byte[] FromUShort2(ushort number)
        {
            var buffer = new byte[2];
            buffer[0] = (byte)(number >> 8 & 0xFF);
            buffer[1] = (byte)(number >> 0 & 0xFF);
            return buffer;
        }

        /// <summary>
        ///     Converts ASCII bytes to a String
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string ToASCII(Memory<byte> buffer)
            => Encoding.ASCII.GetString(buffer.ToArray());

        /// <summary>
        ///     Converts s little-endian 32 bit integer
        ///     in bytes to Int
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int ToInt(Memory<byte> buffer)
        {
            var span = Bytes.SetLength<byte>(4, buffer, 0x00).Span;
            int r = 0;
            r |= span[0] << 0;
            r |= span[1] << 8;
            r |= span[2] << 16;
            r |= span[3] << 24;
            return r;
        }

        public static uint ToUInt(Memory<byte> buffer)
        {
            return (uint)ToInt(buffer);
        }

        /// <summary>
        ///     Converts a String to ASCII bytes,
        ///     prefixed by 1 byte for it's size
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Memory<byte> ToSizedASCII(string str, int length = 0)
        {
            var bytes = Encoding.ASCII.GetBytes(str);

            if (length is 0)
                length = bytes.Length;

            var r = new Memory<byte>(new byte[length + 1]);
            r.Span[0] = (byte)length;
            if (bytes.Length > length)
                bytes[..length].CopyTo(r[1..]);
            else
                bytes.CopyTo(r[1..]);

            return r;

            /*
            yield return (byte)str.Length;
            foreach (int index in Enumerable.Range(0, length))
                if (index >= bytes.Length) yield return 0x00;
                else yield return bytes[index];
            */
        }

        /// <summary>
        ///     Converts a little-endian 16 bit integer
        ///     in bytes to Short
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static ushort ToUShort(Memory<byte> buffer)
        {
            var span = SetLength<byte>(2, buffer, 0x00).Span;
            int r = 0;
            r |= span[0] << 0;
            r |= span[1] << 8;
            return (ushort)r;
        }
    }
}
