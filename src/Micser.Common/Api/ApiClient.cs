using Micser.Common.Extensions;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

#pragma warning disable 4014

namespace Micser.Common.Api
{
    public class ApiClient : ApiEndPoint, IApiClient
    {
        private bool _isConnecting;

        public ApiClient(IRequestProcessorFactory requestProcessorFactory)
            : base(requestProcessorFactory)
        {
        }

        public override async Task ConnectAsync()
        {
            if (_isConnecting)
            {
                return;
            }

            try
            {
                _isConnecting = true;

                _inClient?.Dispose();
                _outClient?.Dispose();

                _outClient = new TcpClient();
                await _outClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                _outClient.Client.SetKeepAlive();
                var outStream = _outClient.GetStream();
                _outReader = new StreamReader(outStream);
                _outWriter = new StreamWriter(outStream) { AutoFlush = true };

                _inClient = new TcpClient();
                await _inClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                _inClient.Client.SetKeepAlive();
                var inStream = _inClient.GetStream();
                _inReader = new StreamReader(inStream);
                _inWriter = new StreamWriter(inStream) { AutoFlush = true };

                Task.Run(() => ReaderThread());
            }
            catch (Exception ex)
            {
                //todo
                Console.WriteLine(ex);
            }
            finally
            {
                _isConnecting = false;
            }
        }
    }
}