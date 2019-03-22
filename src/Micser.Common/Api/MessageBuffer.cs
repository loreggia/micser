using Micser.Common.Extensions;
using System;
using System.Net.Sockets;
using System.Text;

namespace Micser.Common.Api
{
    public class MessageBuffer
    {
        public const string EndOfMessage = "<EOM>";

        public readonly byte[] Buffer;
        public readonly Socket Socket;
        private readonly Action<StringBuilder> _receiveCallback;

        private readonly StringBuilder _result;

        public MessageBuffer(Socket socket, Action<StringBuilder> receiveCallback)
        {
            Buffer = new byte[1024];
            _result = new StringBuilder();
            Socket = socket;
            _receiveCallback = receiveCallback;
        }

        public void BeginReceive()
        {
            Socket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReceiveCallback, this);
        }

        public StringBuilder GetText()
        {
            return new StringBuilder(_result.ToString(0, _result.Length - EndOfMessage.Length));
        }

        public bool ProcessBuffer(int bytesRead)
        {
            var content = Encoding.UTF8.GetChars(Buffer, 0, bytesRead);
            _result.Append(content);
            if (_result.EndsWith(EndOfMessage))
            {
                return true;
            }

            return false;
        }

        private void OnMessageReceived(StringBuilder content)
        {
            _receiveCallback(content);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var bytesRead = Socket.EndReceive(ar);

            if (bytesRead > 0)
            {
                if (ProcessBuffer(bytesRead))
                {
                    OnMessageReceived(GetText());
                    _result.Clear();
                }
            }

            BeginReceive();
        }
    }
}