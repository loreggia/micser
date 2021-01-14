using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micser.DataAccess.Entities
{
    /// <summary>
    /// Storage class for module connections.
    /// </summary>
    public class ModuleConnectionEntity : Entity
    {
        /// <summary>
        /// Gets or sets the name of the connector on the source widget.
        /// </summary>
        [Required]
        public string SourceConnectorName { get; set; } = null!;

        /// <summary>
        /// Gets or set the source module. Navigation property for <see cref="SourceModuleId"/>.
        /// </summary>
        public virtual ModuleEntity SourceModule { get; set; } = null!;

        /// <summary>
        /// Gets or sets the source module foreign key.
        /// </summary>
        [Required]
        [ForeignKey(nameof(SourceModule))]
        public long SourceModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the connector on the target widget.
        /// </summary>
        [Required]
        public string TargetConnectorName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the target module. Navigation property for <see cref="TargetModuleId"/>.
        /// </summary>
        public virtual ModuleEntity TargetModule { get; set; } = null!;

        /// <summary>
        /// Gets or sets the target module foreign key.
        /// </summary>
        [Required]
        [ForeignKey(nameof(TargetModule))]
        public long TargetModuleId { get; set; }
    }
}