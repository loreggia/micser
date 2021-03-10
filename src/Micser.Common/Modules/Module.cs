namespace Micser.Common.Modules
{
    /// <summary>
    /// Module.
    /// </summary>
    public class Module : IIdentifiable
    {
        /// <summary>
        /// Creates an instance of the <see cref="Module"/> type.
        /// </summary>
        /// <param name="id">The module ID.</param>
        /// <param name="type">The module type name.</param>
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
        public ModuleState State { get; }

        /// <summary>
        /// Gets or sets the type/identifier of the module.
        /// </summary>
        public string Type { get; set; }
    }
}