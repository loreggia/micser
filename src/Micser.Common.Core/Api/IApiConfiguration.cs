namespace Micser.Common.Api
{
    /// <summary>
    /// Contains configuration properties for the API end points.
    /// </summary>
    public interface IApiConfiguration
    {
        /// <summary>
        /// Gets the main TCP port to use for API communications.
        /// </summary>
        int Port { get; }
    }
}