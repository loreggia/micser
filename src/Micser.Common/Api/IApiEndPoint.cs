using System.Threading.Tasks;

namespace Micser.Common.Api
{
    public enum EndPointState
    {
        Disconnected,
        Disconnecting,
        Connected,
        Connecting
    }

    /// <summary>
    /// Describes an API component that can send messages to a receiving end point.
    /// </summary>
    public interface IApiEndPoint
    {
        EndPointState State { get; }

        Task<bool> ConnectAsync();

        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="message">The message.</param>
        Task<JsonResponse> SendMessageAsync(JsonRequest message);
    }
}