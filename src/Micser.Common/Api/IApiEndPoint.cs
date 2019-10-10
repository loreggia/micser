namespace Micser.Common.Api
{
    /// <summary>
    /// Describes an API component that can send messages to a receiving end point.
    /// </summary>
    public interface IApiEndPoint
    {
        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        ConnectionState ConnectionState { get; }
    }
}