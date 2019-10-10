using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public interface IApiClient : IApiEndPoint
    {
        /// <summary>
        /// Tries to connect to the remote end point.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects the client connection.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="request">The request message.</param>
        Task<ApiResponse> SendMessageAsync(ApiRequest request);
    }
}