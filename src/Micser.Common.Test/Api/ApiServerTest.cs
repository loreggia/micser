using Micser.Common.Api;
using Micser.TestCommon;
using NLog;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    [Trait("Category", "Network")]
    public class ApiServerTest
    {
        public ApiServerTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputLogger.Configure(testOutputHelper);
        }

        [Fact]
        public async Task ConnectMultipleClients_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server"));
            using var client1 = new ApiClient(configuration, LogManager.GetLogger("Client1"));
            using var client2 = new ApiClient(configuration, LogManager.GetLogger("Client2"));

            var result = await server.StartAsync();
            Assert.True(result);
            result = await client1.ConnectAsync();
            Assert.True(result);
            result = await client2.ConnectAsync();
            Assert.False(result);
        }

        [Fact]
        public async Task StartServer_IsSuccess()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server"));

            var result = await server.StartAsync();

            Assert.True(result);
            Assert.Equal(ServerState.Started, server.State);
        }

        [Fact]
        public async Task StartServerWithDifferentName_IsSuccess()
        {
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server1 = new ApiServer(new ApiConfiguration("Micser.Test.1"), factory, LogManager.GetLogger("Server1"));
            using var server2 = new ApiServer(new ApiConfiguration("Micser.Test.2"), factory, LogManager.GetLogger("Server2"));

            var result1 = await server1.StartAsync();
            var result2 = await server2.StartAsync();

            Assert.True(result1);
            Assert.True(result2);
            Assert.Equal(ServerState.Started, server1.State);
            Assert.Equal(ServerState.Started, server2.State);
        }

        [Fact]
        public async Task StartServerWithSameName_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();

            using var server1 = new ApiServer(configuration, factory, LogManager.GetLogger("Server1"));
            using var server2 = new ApiServer(configuration, factory, LogManager.GetLogger("Server2"));

            var result1 = await server1.StartAsync();
            var result2 = await server2.StartAsync();

            Assert.True(result1);
            Assert.False(result2);
            Assert.Equal(ServerState.Started, server1.State);
            Assert.Equal(ServerState.Stopped, server2.State);
        }
    }
}