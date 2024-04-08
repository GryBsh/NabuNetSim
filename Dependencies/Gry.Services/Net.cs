using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gry
{
    public static partial class Net
    {
        /// <summary>
        ///    Creates a Socket suitable for most TCP serial connections.
        /// </summary>
        /// <param name="noDelay">Optional, default true: enables the NoDelay </param>
        /// <param name="sBufferSize">Optional, default 8 bytes: Send Buffer size</param>
        /// <param name="rBufferSize">Optional, default 8 bytes: Receive Buffer size</param>
        /// <returns>A Socket</returns>
        public static Socket SerialSocket(bool noDelay = true, int sBufferSize = 8, int rBufferSize = 8)
        {
            var socket =
                new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                )
                {
                    NoDelay = noDelay,
                    SendBufferSize = sBufferSize,
                    ReceiveBufferSize = rBufferSize,
                    LingerState = new LingerOption(false, 0)
                };
            return socket;
        }

        public static bool IsHttp(string path)
            => Http().IsMatch(path);

        [GeneratedRegex("[hH][tT]{2}[pP][sS]?://.*")]
        private static partial Regex Http();
    }
}
