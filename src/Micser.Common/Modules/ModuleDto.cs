namespace Micser.Common.Modules
{
    /// <summary>
    /// DTO representing a module.
    /// </summary>
    public class ModuleDto : IIdentifiable
    {
        /// <summary>
        /// Gets or sets the ID of this module.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the module's class.
        /// </summary>
        public string ModuleType { get; set; }

        /// <summary>
        /// Gets or sets the module's current state.
        /// </summary>
        public ModuleState State { get; set; }

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the module's UI/widget class.
        /// </summary>
        public string WidgetType { get; set; }
    }
}