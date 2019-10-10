using System.Threading.Tasks;

namespace Micser.Common.Api
{
    /// <summary>
    /// An API endpoint that acts as the server when establishing a connection.
    /// </summary>
    public interface IApiServer
    {
        /// <summary>
        /// Gets the current server state.
        /// </summary>
        ServerState State { get; }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        Task<bool> StartAsync();

        /// <summary>
        /// Stops listening for connections.
        /// </summary>
        void Stop();
    }
}