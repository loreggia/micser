using Micser.Common.Api;
using Micser.TestCommon;
using NLog;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class ApiClientTest
    {
        public ApiClientTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputLogger.Configure(testOutputHelper);
        }

        [Fact]
        public async Task ConnectWithoutServer_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            using var client = new ApiClient(configuration, LogManager.GetLogger("Client"));

            var result = await client.ConnectAsync();

            Assert.False(result);
            Assert.Equal(ConnectionState.Disconnected, client.ConnectionState);
        }

        [Fact]
        public async Task SendMessageWithClientConnect_IsSuccess()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, LogManager.GetLogger("Client"));

            var startResult = await server.StartAsync();
            Assert.True(startResult);
            Assert.Equal(ServerState.Started, server.State);

            var connectResult = await client.ConnectAsync();
            Assert.True(connectResult);
            Assert.Equal(ConnectionState.Connected, client.ConnectionState);

            var response = await client.SendMessageAsync(new ApiRequest("Resource", "Action", "Content"));
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.Equal("Content", response.Content);
        }

        [Fact]
        public async Task SendMessageWithoutClientConnect_IsSuccess()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, LogManager.GetLogger("Client"));

            var startResult = await server.StartAsync();
            Assert.True(startResult);

            var response = await client.SendMessageAsync(new ApiRequest("Resource", "Action", "Content"));
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.Equal("Content", response.Content);
        }

        [Fact]
        public async Task SendMessageWithoutServer_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();

            using var client = new ApiClient(configuration, LogManager.GetLogger("Client"));
            var response = await client.SendMessageAsync(new ApiRequest("Resource", "Action", "Content"));

            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
        }

        private class TestObject
        {
            public double[] Values { get; set; }
        }
    }
}