using Micser.Common.Api;
using Moq;
using NLog;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class ApiServerTest
    {
        public ApiServerTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelperTarget.ConfigureLogger(testOutputHelper);
        }

        [Fact]
        public async Task ClientReconnect()
        {
            var factory = GetFactory();
            using (var server = new ApiServer(factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(factory, LogManager.GetCurrentClassLogger()))
            {
                server.Start();

                await client.ConnectAsync();

                server.Stop();

                server.Start();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
            }

            await Task.Delay(1000);
        }

        [Fact]
        public async Task SendClientToServer()
        {
            var factory = GetFactory();
            using (var server = new ApiServer(factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(factory, LogManager.GetCurrentClassLogger()))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
            }

            await Task.Delay(1000);
        }

        [Fact]
        public async Task SendServerToClient()
        {
            var factory = GetFactory();
            using (var server = new ApiServer(factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(factory, LogManager.GetCurrentClassLogger()))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await server.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);
            }

            await Task.Delay(1000);
        }

        [Fact]
        public async Task ServerReconnect()
        {
            var factory = GetFactory();
            using (var server = new ApiServer(factory, LogManager.GetCurrentClassLogger()))
            using (var client = new ApiClient(factory, LogManager.GetCurrentClassLogger()))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);

                client.Dispose();

                using (var client2 = new ApiClient(factory, LogManager.GetCurrentClassLogger()))
                {
                    await client2.ConnectAsync();

                    result = await client2.SendMessageAsync(new JsonRequest());

                    Assert.NotNull(result);
                    Assert.True(result.IsSuccess);
                }
            }

            await Task.Delay(1000);
        }

        private static IRequestProcessorFactory GetFactory()
        {
            var processorMock = new Mock<IRequestProcessor>();
            processorMock.Setup(p => p.Process(It.IsAny<string>(), It.IsAny<object>())).Returns<string, object>((n, c) => new JsonResponse(true, c));
            var factoryMock = new Mock<IRequestProcessorFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(processorMock.Object);
            return factoryMock.Object;
        }
    }
}