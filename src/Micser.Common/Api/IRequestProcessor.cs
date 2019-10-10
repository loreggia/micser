using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Classes implementing this interface can receive incoming API requests for processing.
    /// </summary>
    public interface IRequestProcessor
    {
        /// <summary>
        /// Processes the incoming message and returns a response to send back.
        /// </summary>
        /// <param name="action">The action name that was sent in <see cref="ApiRequest.Action"/>.</param>
        /// <param name="content">The deserialized message content.</param>
        /// <returns>A <see cref="ApiResponse"/> that will be sent back.</returns>
        Task<ApiResponse> ProcessAsync(string action, object content);
    }
}