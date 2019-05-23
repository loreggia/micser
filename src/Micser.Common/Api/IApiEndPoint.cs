using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Describes an API component that can send messages to a receiving end point.
    /// </summary>
    public interface IApiEndPoint
    {
        Task<bool> ConnectAsync();

        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="message">The message.</param>
        Task<JsonResponse> SendMessageAsync(JsonRequest message);
    }
}