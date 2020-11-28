using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common.Api;

namespace Micser.Engine.Api
{
    public class EngineEventsApiService : EngineEventsRpcService.EngineEventsRpcServiceBase
    {
        private readonly IRpcStreamService<EngineEvent> _clients;

        public EngineEventsApiService(IRpcStreamService<EngineEvent> clients)
        {
            _clients = clients;
        }

        public override Task Connect(Empty request, IServerStreamWriter<EngineEvent> responseStream, ServerCallContext context)
        {
            var connectionId = context.GetHttpContext().Connection.Id;
            _clients.AddClient(connectionId, responseStream);
            return Task.CompletedTask;
        }
    }
}