namespace Micser.Common.Api
{
    public enum ServerState
    {
        Stopped,
        Stopping,
        Started,
        Starting
    }

    /// <summary>
    /// An API endpoint that acts as the server when establishing a connection.
    /// </summary>
    public interface IApiServer : IApiEndPoint
    {
        ServerState ServerState { get; }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        bool Start();

        /// <summary>
        /// Stops listening for connections.
        /// </summary>
        void Stop();
    }
}