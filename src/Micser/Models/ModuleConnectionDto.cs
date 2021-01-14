namespace Micser.Models
{
    public class ModuleConnectionDto
    {
        public long Id { get; set; }
        public string? SourceConnectorName { get; set; }
        public long SourceId { get; set; }
        public string? TargetConnectorName { get; set; }
        public long TargetId { get; set; }
    }
}