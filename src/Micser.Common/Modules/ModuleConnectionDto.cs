namespace Micser.Common.Modules
{
    public class ModuleConnectionDto : IIdentifiable
    {
        public long Id { get; set; }
        public string SourceConnectorName { get; set; }
        public long SourceId { get; set; }
        public string TargetConnectorName { get; set; }
        public long TargetId { get; set; }
    }
}