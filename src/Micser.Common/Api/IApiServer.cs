using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// An API endpoint that acts as the server when establishing a connection.
    /// </summary>
    public interface IApiServer : IApiEndPoint
    {
        /// <summary>
        /// Gets the current server state.
        /// </summary>
        ServerState ServerState { get; }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        Task<bool> StartServerAsync();

        /// <summary>
        /// Stops listening for connections.
        /// </summary>
        void StopServer();
    }
}