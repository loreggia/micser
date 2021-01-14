using Micser.Common.Modules;

namespace Micser.Models
{
    /// <summary>
    /// DTO used for serializing module data.
    /// </summary>
    public class ModulesExportDto
    {
        public ModulesExportDto(Module[] modules, ModuleConnection[] connections)
        {
            Modules = modules;
            Connections = connections;
        }

        /// <summary>
        /// Gets or sets the connections between the modules.
        /// </summary>
        public ModuleConnection[] Connections { get; }

        /// <summary>
        /// Gets or sets the modules.
        /// </summary>
        public Module[] Modules { get; }
    }
}