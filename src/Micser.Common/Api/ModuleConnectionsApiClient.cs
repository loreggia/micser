using Grpc.Core;

namespace Micser.Common.Api
{
    public class ModuleConnectionsApiClient : ModuleConnectionsRpcService.ModuleConnectionsRpcServiceClient
    {
        public ModuleConnectionsApiClient(ChannelBase channel)
            : base(channel)
        {
        }
    }
}