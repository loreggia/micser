using Grpc.Core;

namespace Micser.Common.Api
{
    public class ModulesApiClient : ModulesRpcService.ModulesRpcServiceClient
    {
        public ModulesApiClient(ChannelBase channel)
            : base(channel)
        {
        }
    }
}