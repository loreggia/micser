using Micser.Common.Api;
using Micser.TestCommon;
using Moq;
using NLog;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Api
{
    public class ApiServerTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ApiServerTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            TestOutputLogger.Configure(testOutputHelper);
        }

        [Fact]
        [Trait("Category", "Network")]
        public async Task ClientReconnect()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ClientReconnect)}: using pipe {configuration.PipeName}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server")))
            using (var client = new ApiClient(configuration, LogManager.GetLogger("Client")))
            {
            }
        }

        [Fact]
        [Trait("Category", "Network")]
        public async Task SendLargeObject()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(SendLargeObject)}: using pipe {configuration.PipeName}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server")))
            using (var client = new ApiClient(configuration, LogManager.GetLogger("Client")))
            {
            }
        }

        [Fact]
        [Trait("Category", "Network")]
        public async Task SendMessage()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ClientReconnect)}: using pipe {configuration.PipeName}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server")))
            using (var client = new ApiClient(configuration, LogManager.GetLogger("Client")))
            {
                var startResult = await server.StartServerAsync();
                Assert.True(startResult);

                var connectResult = await client.ConnectAsync();
                Assert.True(connectResult);

                var response = await client.SendMessageAsync(new ApiRequest("Resource", "Action", "Content"));
                Assert.NotNull(response);
                Assert.True(response.IsSuccess);
                Assert.Equal("Content", response.Content);
            }
        }

        [Fact]
        [Trait("Category", "Network")]
        public async Task ServerReconnect()
        {
            var configuration = GetConfiguration();
            var factory = GetFactory();

            _testOutputHelper.WriteLine($"{nameof(ServerReconnect)}: using pipe {configuration.PipeName}");

            using (var server = new ApiServer(configuration, factory, LogManager.GetLogger("Server")))
            using (var client = new ApiClient(configuration, LogManager.GetLogger("Client1")))
            {
            }
        }

        private static ApiConfiguration GetConfiguration()
        {
            return new ApiConfiguration { PipeName = "Micser.Common.Test" };
        }

        private static IRequestProcessorFactory GetFactory()
        {
            var processorMock = new Mock<IRequestProcessor>();
            processorMock.Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync((string n, object c) => new ApiResponse(true, c));
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