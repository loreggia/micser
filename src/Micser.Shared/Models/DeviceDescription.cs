namespace Micser.Shared.Models
{
    public class DeviceDescription
    {
        public string Id { get; set; }
        public string IconPath { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public DeviceType Type { get; set; }
    }
}