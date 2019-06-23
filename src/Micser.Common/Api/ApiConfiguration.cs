namespace Micser.Common.Api
{
    /// <inheritdoc cref="IApiConfiguration" />
    public class ApiConfiguration : IApiConfiguration
    {
        /// <inheritdoc />
        public int Port { get; set; }
    }
}