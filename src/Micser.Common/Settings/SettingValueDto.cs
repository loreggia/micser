namespace Micser.Common.Settings
{
    /// <summary>
    /// DTO for transferring setting values via API.
    /// </summary>
    public class SettingValueDto
    {
        /// <summary>
        /// The setting key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The setting value.
        /// </summary>
        public object Value { get; set; }
    }
}