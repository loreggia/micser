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

                InClient?.Dispose();
                OutClient?.Dispose();

                OutClient = new TcpClient();
                await OutClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                OutClient.Client.SetKeepAlive();
                var outStream = OutClient.GetStream();
                OutReader = new StreamReader(outStream);
                OutWriter = new StreamWriter(outStream) { AutoFlush = true };

                InClient = new TcpClient();
                await InClient.ConnectAsync(IPAddress.Loopback, Globals.ApiPort);
                InClient.Client.SetKeepAlive();
                var inStream = InClient.GetStream();
                InReader = new StreamReader(inStream);
                InWriter = new StreamWriter(inStream) { AutoFlush = true };

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