namespace Micser.Common.Api
{
    /// <summary>
    /// Defines state the API server can be in.
    /// </summary>
    public enum ServerState
    {
        /// <summary>
        /// Stopped. The server is not listening for connections and cannot be connected to.
        /// </summary>
        Stopped,

        /// <summary>
        /// The <see cref="IApiServer.Stop"/> method has been called on the server but not completed yet.
        /// </summary>
        Stopping,

        /// <summary>
        /// The server is started. Use the <see cref="IApiEndPoint.State"/> property to get the current connection state.
        /// </summary>
        Started,

        /// <summary>
        /// The <see cref="IApiServer.Start"/> method has been called on the server but not completed yet.
        /// </summary>
        Starting
    }
}