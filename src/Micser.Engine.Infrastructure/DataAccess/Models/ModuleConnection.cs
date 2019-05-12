using Micser.Common.DataAccess;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.Engine.Infrastructure.DataAccess.Models
{
    /// <summary>
    /// Storage class for module connections.
    /// </summary>
    public class ModuleConnection : Model
    {
        /// <summary>
        /// Gets or sets the name of the connector on the source widget.
        /// </summary>
        public string SourceConnectorName { get; set; }

        /// <summary>
        /// Gets or set the source module. Navigation property for <see cref="SourceModuleId"/>.
        /// </summary>
        public virtual Module SourceModule { get; set; }

        /// <summary>
        /// Gets or sets the source module foreign key.
        /// </summary>
        [ForeignKey(nameof(SourceModule))]
        public long SourceModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the target widget.
        /// </summary>
        public string TargetConnectorName { get; set; }

        /// <summary>
        /// Gets or sets the target module. Navigation property for <see cref="TargetModuleId"/>.
        /// </summary>
        public virtual Module TargetModule { get; set; }

        /// <summary>
        /// Gets or sets the target module foreign key.
        /// </summary>
        [ForeignKey(nameof(TargetModule))]
        public long TargetModuleId { get; set; }
    }
}