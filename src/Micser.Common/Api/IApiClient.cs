using System;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IApiClient : IDisposable
    {
        Task ConnectAsync();

        Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5);
    }
}