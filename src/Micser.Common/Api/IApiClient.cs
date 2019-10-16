using System.Threading;
using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IApiClient
    {
        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        /// Tries to connect to the remote end point.
        /// </summary>
        Task<bool> ConnectAsync(CancellationToken token);

        /// <summary>
        /// Disconnects the client connection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="token">The cancellation token.</param>
        Task<ApiResponse> SendMessageAsync(ApiRequest request, CancellationToken token);
    }
}