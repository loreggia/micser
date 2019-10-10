using ProtoBuf;

namespace Micser.Common.Settings
{
    /// <summary>
    /// DTO for transferring setting values via API.
    /// </summary>
    [ProtoContract]
    public sealed class SettingValueDto
    {
        /// <summary>
        /// The setting key.
        /// </summary>
        [ProtoMember(1)]
        public string Key { get; set; }

        /// <summary>
        /// The setting value.
        /// </summary>
        [ProtoMember(2, DynamicType = true)]
        public object Value { get; set; }
    }
}