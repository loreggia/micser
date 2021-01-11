namespace Micser.Common.Modules
{
    /// <summary>
    /// DTO representing a module connection.
    /// </summary>
    public class ModuleConnection : IIdentifiable
    {
        /// <summary>
        /// Gets or sets the ID of this connection.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the source module.
        /// </summary>
        public string? SourceConnectorName { get; set; }

        /// <summary>
        /// Gets or sets the source module ID.
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the target module.
        /// </summary>
        public string? TargetConnectorName { get; set; }

        /// <summary>
        /// Gets or sets the target module ID.
        /// </summary>
        public long TargetId { get; set; }
    }
}