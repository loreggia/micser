namespace Micser.Common.Api
{
    /// <summary>
    /// An API endpoint that acts as the server when establishing a connection.
    /// </summary>
    public interface IApiServer : IApiEndPoint
    {
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