namespace Micser.Common.Modules
{
    /// <summary>
    /// DTO that represents the change of a single module state value.
    /// </summary>
    public class ModuleStateChangeDto
    {
        /// <summary>
        /// Gets or sets the state value key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the module ID.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public object Value { get; set; }
    }
}