namespace Micser.Common.Api
{
    /// <inheritdoc cref="IApiConfiguration" />
    public class ApiConfiguration : IApiServerConfiguration, IApiClientConfiguration
    {
        public string PipeName { get; set; }
    }
}