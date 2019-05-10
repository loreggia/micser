using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Describes an API component that can send messages to a receiving end point.
    /// </summary>
    public interface IApiEndPoint
    {
        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="numRetries">The number of retries to try sending the message if sending it failed.</param>
        Task<JsonResponse> SendMessageAsync(JsonRequest message, int numRetries = 5);
    }
}