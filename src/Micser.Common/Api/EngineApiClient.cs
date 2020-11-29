using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Common.Api
{
    public class EngineApiClient : EngineRpcService.EngineRpcServiceClient
    {
        public EngineApiClient(ChannelBase channel)
            : base(channel)
        {
        }

        public async Task<bool> GetStatusAsync()
        {
            var result = await GetStatusAsync(new Empty());
            return result.Value;
        }

        public async Task<bool> RestartAsync()
        {
            var result = await RestartAsync(new Empty());
            return result.Value;
        }

        public async Task<bool> StartAsync()
        {
            var result = await StartAsync(new Empty());
            return result.Value;
        }

        public async Task<bool> StopAsync()
        {
            var result = await StopAsync(new Empty());
            return result.Value;
        }
    }
}