namespace Micser.Common.Modules
{
    /// <summary>
    /// A connection between two modules.
    /// </summary>
    public class ModuleConnection : IIdentifiable
    {
        public ModuleConnection(long id, long sourceId, string sourceConnectorName, long targetId, string targetConnectorName)
        {
            Id = id;
            SourceId = sourceId;
            SourceConnectorName = sourceConnectorName;
            TargetId = targetId;
            TargetConnectorName = targetConnectorName;
        }

        /// <summary>
        /// Gets or sets the ID of this connection.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the source module.
        /// </summary>
        public string SourceConnectorName { get; set; }

        /// <summary>
        /// Gets or sets the source module ID.
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the target module.
        /// </summary>
        public string TargetConnectorName { get; set; }

        /// <summary>
        /// Gets or sets the target module ID.
        /// </summary>
        public long TargetId { get; set; }
    }
}