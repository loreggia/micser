using Micser.Common.Api;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Common.Test.Api
{
    public class ApiServerTest
    {
        [Fact]
        public async Task ClientReconnect()
        {
            using (var server = new ApiServer())
            using (var client = new ApiClient())
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
            using (var server = new ApiServer())
            using (var client = new ApiClient())
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
            using (var server = new ApiServer())
            using (var client = new ApiClient())
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
            using (var server = new ApiServer())
            using (var client = new ApiClient())
            {
                server.Start();

                await client.ConnectAsync();

                var result = await client.SendMessageAsync(new JsonRequest());

                Assert.NotNull(result);
                Assert.True(result.IsSuccess);

                client.Dispose();

                using (var client2 = new ApiClient())
                {
                    await client2.ConnectAsync();

                    result = await client2.SendMessageAsync(new JsonRequest());

                    Assert.NotNull(result);
                    Assert.True(result.IsSuccess);
                }
            }

            await Task.Delay(1000);
        }
    }
}