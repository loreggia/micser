using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// Defines connection states for an <see cref="IApiEndPoint"/>.
    /// </summary>
    public enum EndPointState
    {
        /// <summary>
        /// The end point has no active connection and is not currently connecting.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The end point has lost a connection and is currently cleaning up connection resources.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// The end point is connected and ready to send or receive messages.
        /// </summary>
        Connected,

        /// <summary>
        /// The end point is currently connecting.
        /// </summary>
        Connecting
    }

    /// <summary>
    /// Describes an API component that can send messages to a receiving end point.
    /// </summary>
    public interface IApiEndPoint
    {
        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        EndPointState State { get; }

        /// <summary>
        /// Tries to connect to the other end point.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Sends a message to the connected endpoint.
        /// </summary>
        /// <param name="message">The message.</param>
        Task<JsonResponse> SendMessageAsync(JsonRequest message);
    }
}