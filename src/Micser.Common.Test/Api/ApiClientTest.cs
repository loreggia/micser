using Micser.Common.Api;
using Micser.Common.Extensions;
using Micser.TestCommon;
using NLog;
using System.Diagnostics;
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
        public async Task ConnectDisconnect_SetsState()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var server = new ApiServer(configuration, factory, serializer, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var result = await server.StartAsync();
            Assert.True(result);

            result = await client.ConnectAsync();
            Assert.True(result);
            Assert.Equal(ConnectionState.Connected, client.State);

            client.Disconnect();
            Assert.Equal(ConnectionState.Disconnected, client.State);
        }

        [Fact]
        public async Task ConnectWithoutServer_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var serializer = ApiTestHelper.GetMessageSerializer();
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var result = await client.ConnectAsync();

            Assert.False(result);
            Assert.Equal(ConnectionState.Disconnected, client.State);
        }

        [Fact]
        public async Task SendMessageAfterServerStops_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var server = new ApiServer(configuration, factory, serializer, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var result = await server.StartAsync();
            Assert.True(result);

            result = await client.ConnectAsync();
            Assert.True(result);
            Assert.Equal(ConnectionState.Connected, client.State);

            server.Stop();

            var response = await client.SendMessageAsync("Resource", "Action", "Content");
            Assert.False(response.IsSuccess);
            Assert.Equal(ConnectionState.Disconnected, client.State);
        }

        [Fact]
        public async Task SendMessageOneClientParallelism()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();
            var logger = LogManager.GetCurrentClassLogger();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var server = new ApiServer(configuration, factory, serializer, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var startResult = await server.StartAsync();
            Assert.True(startResult);
            var connectResult = await client.ConnectAsync();
            Assert.True(connectResult);

            const int count = 1000;
            var tasks = new Task<ApiResponse>[count];

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (var i = 0; i < count; i++)
            {
                tasks[i] = client.SendMessageAsync("Parallel", "Action", new TestData { Value = i });
            }

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            logger.Info($"Sent {count} messages. Duration: {stopwatch.Elapsed}");

            for (var i = 0; i < count; i++)
            {
                var response = tasks[i].Result;
                Assert.NotNull(response);
                var testData = response.Content as TestData;
                Assert.NotNull(testData);
                Assert.Equal(i, testData.Value);
            }
        }

        [Fact]
        public async Task SendMessageWithClientConnect_IsSuccess()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var server = new ApiServer(configuration, factory, serializer, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var startResult = await server.StartAsync().ConfigureAwait(false);
            Assert.True(startResult);
            Assert.Equal(ServerState.Started, server.State);

            var connectResult = await client.ConnectAsync().ConfigureAwait(false);
            Assert.True(connectResult);
            Assert.Equal(ConnectionState.Connected, client.State);

            var response = await client.SendMessageAsync("Resource", "Action", "Content").ConfigureAwait(false);
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.Equal("Content", response.Content);
        }

        [Fact]
        public async Task SendMessageWithoutClientConnect_IsSuccess()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var factory = ApiTestHelper.GetRequestProcessorFactory();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var server = new ApiServer(configuration, factory, serializer, LogManager.GetLogger("Server"));
            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));

            var startResult = await server.StartAsync();
            Assert.True(startResult);

            var response = await client.SendMessageAsync("Resource", "Action", "Content");
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
        }

        [Fact]
        public async Task SendMessageWithoutServer_IsFailure()
        {
            var configuration = ApiTestHelper.GetConfiguration();
            var serializer = ApiTestHelper.GetMessageSerializer();

            using var client = new ApiClient(configuration, serializer, LogManager.GetLogger("Client"));
            var response = await client.SendMessageAsync("Resource", "Action", "Content");

            Assert.NotNull(response);
            Assert.False(response.IsSuccess);
        }

        public class TestData
        {
            public int Value { get; set; }

            public double[] Values { get; set; }
        }
    }
}