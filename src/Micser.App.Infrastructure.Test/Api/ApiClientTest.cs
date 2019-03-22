using Micser.App.Infrastructure.Api;
using Micser.Common;
using Micser.Common.Api;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Micser.App.Infrastructure.Test.Api
{
    public class ApiClientTest
    {
        public void TestReceiveMessage()
        {
        }

        [Fact]
        public async Task TestSendMessage()
        {
            var server = CreateServer();
            var acceptEvent = new ManualResetEvent(false);
            var resultEvent = new ManualResetEvent(false);
            StringBuilder result = null;

            Socket connection;
            server.BeginAccept(ar =>
            {
                connection = server.EndAccept(ar);
                var messageBuffer = new MessageBuffer(connection, content =>
                {
                    result = content;
                    resultEvent.Set();
                });
                messageBuffer.BeginReceive();
                acceptEvent.Set();
            }, null);

            var apiClient = new TestApiClient();
            await apiClient.ConnectAsync();

            acceptEvent.WaitOne();

            await apiClient.SendAsync("Test");

            resultEvent.WaitOne();

            Assert.Equal("Test", result.ToString());
        }

        private Socket CreateServer()
        {
            var hostEntry = Dns.GetHostEntry(IPAddress.Loopback);
            var address = hostEntry.AddressList[0];
            var endPoint = new IPEndPoint(address, Globals.ApiPort);
            var result = new Socket(SocketType.Stream, ProtocolType.Tcp);
            result.Bind(endPoint);
            result.Listen(5);
            return result;
        }

        private class TestApiClient : ApiClient
        {
        }
    }
}