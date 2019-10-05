namespace Micser.Common.Api
{
    /// <summary>
    /// Provides functionality to create a request processor for an incoming request.
    /// </summary>
    public interface IRequestProcessorFactory
    {
        /// <summary>
        /// Creates a request processor that was registered using the <see cref="RequestProcessorNameAttribute"/> attribute.
        /// </summary>
        /// <param name="name">The processor name.</param>
        IRequestProcessor Create(string name);
    }
}