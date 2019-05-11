namespace Micser.Common.Devices
{
    /// <summary>
    /// Describes a hardware audio device.
    /// </summary>
    public class DeviceDescription
    {
        /// <summary>
        /// Gets or sets the file path to the icon file to use for this device.
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Gets or sets the hardware device ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets whether this device is active (not disabled and plugged in).
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the friendly display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the device's type (input/output).
        /// </summary>
        public DeviceType Type { get; set; }
    }
}