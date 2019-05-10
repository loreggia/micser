namespace Micser.Common.Api
{
    /// <summary>
    /// An API endpoint that acts as the server when establishing a connection.
    /// </summary>
    public interface IApiServer : IApiEndPoint
    {
        /// <summary>
        /// Gets a value that indicates whether the server is currently listening for connections.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops listening for connections.
        /// </summary>
        void Stop();
    }
}