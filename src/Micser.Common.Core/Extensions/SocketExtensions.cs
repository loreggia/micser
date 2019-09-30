using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Micser.Common.Extensions
{
    /// <summary>
    /// Provides helper extension methods for <see cref="Socket"/> objects.
    /// </summary>
    public static class SocketExtensions
    {
        /// <summary>
        /// Sets the keep-alive time for a socket.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="enabled"></param>
        /// <param name="keepAliveTime"></param>
        /// <param name="keepAliveInterval"></param>
        public static void SetKeepAlive(this Socket socket, bool enabled = true, uint keepAliveTime = 100, uint keepAliveInterval = 100)
        {
            var size = Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(enabled ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(inOptionValues, size * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
    }
}