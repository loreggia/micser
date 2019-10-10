namespace Micser.Common.Api
{
    /// <inheritdoc cref="IApiConfiguration" />
    public class ApiConfiguration : IApiServerConfiguration, IApiClientConfiguration
    {
        public ApiConfiguration()
        {
        }

        public ApiConfiguration(string pipeName)
        {
            PipeName = pipeName;
        }

        public string PipeName { get; set; }
    }
}