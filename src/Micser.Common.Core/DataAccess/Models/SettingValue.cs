namespace Micser.Common.DataAccess.Models
{
    /// <summary>
    /// Contains a JSON-serialized setting value.
    /// </summary>
    public class SettingValue : Model
    {
        /// <summary>
        /// Gets or sets the unique setting key name.
        /// </summary>
        [Index(IsUnique = true)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the serialized value.
        /// </summary>
        public string ValueJson { get; set; }

        /// <summary>
        /// Gets or sets the assembly-qualified type name of the value object.
        /// </summary>
        public string ValueType { get; set; }
    }
}