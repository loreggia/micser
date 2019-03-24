using Micser.Common.Api;
using Micser.Common.Extensions;
using System.Threading.Tasks;
using Unity;
using Xunit;

namespace Micser.Common.Test.Api
{
    public class ApiServerTest
    {
        private readonly IRequestProcessorFactory _factory;

        public ApiServerTest()
        {
            var container = new UnityContainer();
            container.RegisterRequestProcessor<TestProcessor>();
            _factory = new RequestProcessorFactory(container);
        }

        [Fact]
        public async Task ClientReconnect()
        {
            using (var server = new ApiServer(_factory))
            using (var client = new ApiClient(_factory))
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
            using (var server = new ApiServer(_factory))
            using (var client = new ApiClient(_factory))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
            }

            await Task.Delay(1000);
        }

        [Fact]
        public async Task SendServerToClient()
        {
            using (var server = new ApiServer(_factory))
            using (var client = new ApiClient(_factory))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await server.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
            }

            await Task.Delay(1000);
        }

        [Fact]
        public async Task ServerReconnect()
        {
            using (var server = new ApiServer(_factory))
            using (var client = new ApiClient(_factory))
            {
                server.Start();

                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);

                client.Dispose();

                using (var client2 = new ApiClient(_factory))
                {
                    await client2.ConnectAsync();

                    result = await client2.SendMessageAsync(new JsonRequest());

                    Assert.NotNull(result);
                    Assert.True(result.IsSuccess);
                }
            }

            await Task.Delay(1000);
        }

        public class TestProcessor : IRequestProcessor
        {
            public JsonResponse Process(string action, object content)
            {
                return new JsonResponse(true, null, null);
            }
        }
    }
}