using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Micser.Common.Api;

namespace Micser.Engine.Api
{
    public class EngineEventsApiService : EngineEventsRpcService.EngineEventsRpcServiceBase
    {
        private readonly ConcurrentDictionary<string, IServerStreamWriter<EngineEvent>> _connections;

        public EngineEventsApiService()
        {
            _connections = new ConcurrentDictionary<string, IServerStreamWriter<EngineEvent>>();
        }

        public override Task Connect(Empty request, IServerStreamWriter<EngineEvent> responseStream, ServerCallContext context)
        {
            var connectionId = context.GetHttpContext().Connection.Id;
            _connections.AddOrUpdate(connectionId, responseStream, (_, __) => responseStream);
            return Task.CompletedTask;
        }

        public async Task SendEvent(string type, string content = "")
        {
            var evt = new EngineEvent
            {
                Type = type ?? throw new ArgumentNullException(nameof(type)),
                Content = content ?? ""
            };

            var tasks = new List<Task>();
            var disconnected = new List<string>();

            foreach (var (id, connection) in _connections)
            {
                try
                {
                    tasks.Add(connection.WriteAsync(evt));
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