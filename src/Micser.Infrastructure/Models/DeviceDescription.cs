namespace Micser.Infrastructure.Models
{
    public class DeviceDescription
    {
        public string IconPath { get; set; }
        public string Id { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public DeviceType Type { get; set; }
    }
}