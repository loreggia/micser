namespace Micser.Common.Modules
{
    /// <summary>
    /// Module.
    /// </summary>
    public class Module : IIdentifiable
    {
        public Module(long id, string type)
        {
            Id = id;
            Type = type;
            State = new ModuleState();
        }

        /// <summary>
        /// Gets or sets the ID of this module.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the module's current state.
        /// </summary>
        public ModuleState State { get; set; }

        /// <summary>
        /// Gets or sets the type/identifier of the module.
        /// </summary>
        public string Type { get; set; }
    }
}