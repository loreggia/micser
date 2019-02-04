namespace Micser.Common.Modules
{
    public class ModuleConnectionDto
    {
        public ModuleConnectionDto()
        {
        }

        public long Id { get; set; }
        public long SourceId { get; set; }
        public long TargetId { get; set; }
    }
}