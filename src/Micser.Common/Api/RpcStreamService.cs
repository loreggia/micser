using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Common.Api
{
    public class RpcStreamService<TMessage> : IRpcStreamService<TMessage>
    {
        private readonly ConcurrentDictionary<string, IAsyncStreamWriter<TMessage>> _clients;

        public RpcStreamService()
        {
            _clients = new ConcurrentDictionary<string, IAsyncStreamWriter<TMessage>>();
        }

        public void AddClient(string id, IAsyncStreamWriter<TMessage> clientStream)
        {
            _clients.AddOrUpdate(id, clientStream, (_, __) => clientStream);
        }

        public async Task SendMessageAsync(TMessage message)
        {
            var tasks = new List<Task>();

            foreach (var (id, connection) in _clients)
            {
                try
                {
                    tasks.Add(connection.WriteAsync(message));
                }
                catch
                {
                    _clients.Remove(id, out _);
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}