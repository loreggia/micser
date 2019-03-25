using System;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IApiServer : IDisposable
    {
        Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5);

        void Start();

        void Stop();
    }
}