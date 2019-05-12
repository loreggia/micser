using Micser.Common.DataAccess;
using Micser.Common.Modules;
using System.Collections.Generic;

namespace Micser.Engine.Infrastructure.DataAccess.Models
{
    /// <summary>
    /// Storage class for modules.
    /// </summary>
    public class Module : Model
    {
        /// <summary>
        /// Gets or sets the assembly-qualified type of the module.
        /// </summary>
        public string ModuleType { get; set; }

        /// <summary>
        /// Navigation property to the <see cref="ModuleConnection"/> class. Gets or sets connections that this module is the source of.
        /// </summary>
        public virtual ICollection<ModuleConnection> SourceModuleConnections { get; set; }

        /// <summary>
        /// Gets or sets a serialized <see cref="ModuleState"/> object.
        /// </summary>
        public string StateJson { get; set; }

        /// <summary>
        /// Navigation property to the <see cref="ModuleConnection"/> class. Gets or sets connections that this module is the target of.
        /// </summary>
        public virtual ICollection<ModuleConnection> TargetModuleConnections { get; set; }

        /// <summary>
        /// Gets or sets the assembly-qualified type of the module's widget.
        /// </summary>
        public string WidgetType { get; set; }
    }
}