namespace Micser.Common.Api
{
    /// <summary>
    /// Defines connection states for an <see cref="IApiClient"/>.
    /// </summary>
    public enum ConnectionState
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
}