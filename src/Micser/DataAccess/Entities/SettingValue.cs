namespace Micser.DataAccess.Entities
{
    /// <summary>
    /// Contains a JSON-serialized setting value.
    /// </summary>
    public class SettingValue : Entity
    {
        /// <summary>
        /// Gets or sets the unique setting key name.
        /// </summary>
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets the serialized value.
        /// </summary>
        public string ValueJson { get; set; } = null!;

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the value object.
        /// </summary>
        public string ValueType { get; set; } = null!;
    }
}