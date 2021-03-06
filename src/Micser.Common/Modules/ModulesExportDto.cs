﻿namespace Micser.Common.Modules
{
    /// <summary>
    /// DTO used for serializing module data.
    /// </summary>
    public class ModulesExportDto
    {
        /// <summary>
        /// Gets or sets the connections between the modules.
        /// </summary>
        public ModuleConnectionDto[] Connections { get; set; }

        /// <summary>
        /// Gets or sets the modules.
        /// </summary>
        public ModuleDto[] Modules { get; set; }
    }
}