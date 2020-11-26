using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Plugins.Main.Api
{
    public class SpectrumApiService : SpectrumRpcService.SpectrumRpcServiceBase
    {
        private readonly ConcurrentDictionary<string, IServerStreamWriter<SpectrumData>> _connections;

        public SpectrumApiService()
        {
            _connections = new ConcurrentDictionary<string, IServerStreamWriter<SpectrumData>>();
        }

        public override Task Connect(Empty request, IServerStreamWriter<SpectrumData> responseStream, ServerCallContext context)
        {
            var connectionId = context.GetHttpContext().Connection.Id;
            _connections.AddOrUpdate(connectionId, responseStream, (_, __) => responseStream);
            return Task.CompletedTask;
        }

        public async Task SendEvent(SpectrumData data)
        {
            var tasks = new List<Task>();
            var disconnected = new List<string>();

            foreach (var (id, connection) in _connections)
            {
                try
                {
                    tasks.Add(connection.WriteAsync(data));
                }
                catch
                {
                    disconnected.Add(id);
                }
            }

            disconnected.RemoveAll(disconnected.Contains);
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}