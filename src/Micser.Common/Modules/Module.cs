namespace Micser.Common.Modules
{
    /// <summary>
    /// DTO representing a module.
    /// </summary>
    public class Module : IIdentifiable
    {
        public Module()
        {
            ModuleType = "Unknown";
            State = new ModuleState();
        }

        /// <summary>
        /// Gets or sets the ID of this module.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the type/identifier of the module.
        /// </summary>
        public string ModuleType { get; set; }

        /// <summary>
        /// Gets or sets the module's current state.
        /// </summary>
        public ModuleState State { get; set; }
    }
}