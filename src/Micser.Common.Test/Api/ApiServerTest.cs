using Micser.Common.Api;
using Moq;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class ApiServerTest
    {
        private static int _currentPort = Globals.ApiPort;
        private readonly ITestOutputHelper _testOutputHelper;

        public ApiServerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            TestOutputHelperTarget.ConfigureLogger(testOutputHelper);
        }

        [Fact]
        public async Task BiDirectionalMessaging()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ClientReconnect)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                var startResult = server.Start();

                Assert.True(startResult);

                var serverTask = server.ConnectAsync();
                var clientTask = client.ConnectAsync();

                await Task.WhenAll(serverTask, clientTask);

                Assert.True(serverTask.Result);

                var clientResponseTasks = new List<Task<JsonResponse>>();
                var serverResponseTasks = new List<Task<JsonResponse>>();

                for (int i = 0; i < 100; i++)
                {
                    var clientResponseTask = server.SendMessageAsync(new JsonRequest { Content = new { Property = "server-to-client" } });
                    clientResponseTasks.Add(clientResponseTask);
                    var serverResponseTask = client.SendMessageAsync(new JsonRequest { Content = new { Property = "client-to-server" } });
                    serverResponseTasks.Add(serverResponseTask);
                }

                await Task.WhenAll(clientResponseTasks);
                await Task.WhenAll(serverResponseTasks);

                Assert.All(clientResponseTasks, task =>
                {
                    Assert.NotNull(task.Result);
                    Assert.True(task.Result.IsSuccess, task.Result.Message);
                });
                Assert.All(serverResponseTasks, task =>
                {
                    Assert.NotNull(task.Result);
                    Assert.True(task.Result.IsSuccess, task.Result.Message);
                });
            }
        }

        //[Fact]
        public async Task ClientReconnect()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ClientReconnect)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                var startResult = server.Start();

                Assert.True(startResult);

                var serverTask = server.ConnectAsync();
                var clientTask = client.ConnectAsync();

                await Task.WhenAll(serverTask, clientTask);

                Assert.True(serverTask.Result);

                server.Stop();

                server.Start();

                await server.ConnectAsync();
                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess, result.Message);
            }
        }

        [Fact]
        public async Task SendClientToServer()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(SendClientToServer)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                var startResult = server.Start();

                Assert.True(startResult);

                var serverTask = server.ConnectAsync();
                var clientTask = client.ConnectAsync();
                await Task.WhenAll(serverTask, clientTask);

                Assert.True(serverTask.Result);

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess, result.Message);
            }
        }

        [Fact]
        public async Task SendLargeObject()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(SendLargeObject)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                var startResult = server.Start();

                Assert.True(startResult);

                var serverTask = server.ConnectAsync();
                var clientTask = client.ConnectAsync();
                await Task.WhenAll(serverTask, clientTask);

                Assert.True(serverTask.Result);

                var valueCount = 100000;
                var content = new TestObject
                {
                    Values = new double[valueCount]
                };
                for (int i = 0; i < valueCount; i++)
                {
                    content.Values[i] = i;
                }

                var stopwatch = Stopwatch.StartNew();
                var result = await client.SendMessageAsync(new JsonRequest("test", null, content));
                stopwatch.Stop();

                _testOutputHelper.WriteLine($"Sending {valueCount} values took {stopwatch.Elapsed}");

                Assert.NotNull(result);
                Assert.True(result.IsSuccess, result.Message);
            }
        }

        [Fact]
        public async Task SendServerToClient()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(SendServerToClient)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                var startResult = server.Start();

                Assert.True(startResult);

                var serverTask = server.ConnectAsync();
                var clientTask = client.ConnectAsync();
                await Task.WhenAll(serverTask, clientTask);

                Assert.True(serverTask.Result);

                var result = await server.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess, result.Message);
            }
        }

        [Fact]
        public async Task ServerReconnect()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ServerReconnect)}: using port {configuration.Port}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
            {
                server.Start();

                var clientTask = client.ConnectAsync();
                var serverTask = server.ConnectAsync();

                await Task.WhenAll(clientTask, serverTask);

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess, result.Message);

                client.Dispose();

                using (var client2 = new ApiClient(configuration, factory, LogManager.GetCurrentClassLogger()))
                {
                    clientTask = client2.ConnectAsync();
                    serverTask = server.ConnectAsync();

                    await Task.WhenAll(clientTask, serverTask);

                    result = await client2.SendMessageAsync(new JsonRequest());

                    Assert.NotNull(result);
                    Assert.True(result.IsSuccess, result.Message);
                }
            }
        }

        private static IApiConfiguration GetConfiguration()
        {
            return new ApiConfiguration
            {
                Port = _currentPort++
            };
        }

        private static IRequestProcessorFactory GetFactory()
        {
            var processorMock = new Mock<IRequestProcessor>();
            processorMock.Setup(p => p.Process(It.IsAny<string>(), It.IsAny<object>())).Returns<string, object>((n, c) => new JsonResponse(true, c));
            var factoryMock = new Mock<IRequestProcessorFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(processorMock.Object);
            return factoryMock.Object;
        }

        private class TestObject
        {
            public double[] Values { get; set; }
        }
    }
}