using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Micser.Common.Modules;

namespace Micser.DataAccess.Entities
{
    /// <summary>
    /// Storage class for modules.
    /// </summary>
    public class ModuleEntity : Entity
    {
        /// <summary>
        /// Navigation property to the <see cref="ModuleConnectionEntity"/> class. Gets or sets connections that this module is the source of.
        /// </summary>
        public virtual ICollection<ModuleConnectionEntity> SourceModuleConnections { get; set; } = null!;

        /// <summary>
        /// Gets or sets a serialized <see cref="ModuleState"/> object.
        /// </summary>
        [Required]
        public string StateJson { get; set; } = null!;

        /// <summary>
        /// Navigation property to the <see cref="ModuleConnectionEntity"/> class. Gets or sets connections that this module is the target of.
        /// </summary>
        public virtual ICollection<ModuleConnectionEntity> TargetModuleConnections { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type/identifier of the module.
        /// </summary>
        [Required]
        public string Type { get; set; } = null!;
    }
}