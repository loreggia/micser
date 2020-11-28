using System.Threading.Tasks;
using Grpc.Core;

namespace Micser.Common.Api
{
    public interface IRpcStreamService<TMessage>
    {
        void AddClient(string id, IAsyncStreamWriter<TMessage> clientStream);

        Task SendMessageAsync(TMessage message);
    }
}