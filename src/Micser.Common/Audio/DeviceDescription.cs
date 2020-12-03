namespace Micser.Common.Audio
{
    /// <summary>
    /// Describes a hardware audio device.
    /// </summary>
    public class DeviceDescription
    {
        /// <summary>
        /// Gets or sets the device adapter name (i.e. "High Definition Audio Device").
        /// </summary>
        public string AdapterName { get; set; }

        /// <summary>
        /// Gets or sets the device description (i.e. "Headphones", "Speakers").
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the friendly display name.
        /// </summary>
        public string FriendlyName { get; set; }

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
        /// Gets or sets the device's type (input/output).
        /// </summary>
        public DeviceType Type { get; set; }
    }
}