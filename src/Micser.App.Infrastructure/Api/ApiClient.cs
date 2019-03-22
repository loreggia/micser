using Micser.Common;
using Micser.Common.Api;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Api
{
    public abstract class ApiClient : IDisposable
    {
        protected readonly ILogger Logger;
        private static Socket _socket;
        private bool _isConnecting;

        protected ApiClient()
        {
            //Logger = logger;
        }

        public bool IsConnected => _socket?.Connected == true;

        public async Task ConnectAsync()
        {
            if (IsConnected || _isConnecting)
            {
                return;
            }

            try
            {
                var hostEntry = Dns.GetHostEntry(IPAddress.Loopback);
                var address = hostEntry.AddressList[0];
                var endPoint = new IPEndPoint(address, Globals.ApiPort);

                _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _isConnecting = true;
                await _socket.ConnectAsync(endPoint);

                var buffer = new MessageBuffer(_socket, OnMessageReceived);
                buffer.BeginReceive();
            }
            catch (Exception ex)
            {
                //Logger.Error(ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task SendAsync(string content)
        {
            await _socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(content + MessageBuffer.EndOfMessage)), SocketFlags.None);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _socket.Close();
                _socket.Dispose();
            }
        }

        private void OnMessageReceived(StringBuilder content)
        {
        }
    }
}