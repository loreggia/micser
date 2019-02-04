namespace Micser.Common.Modules
{
    public class ModuleConnectionDto
    {
        public ModuleConnectionDto()
        {
        }

        public ModuleConnectionDto(long sourceId, long targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
        }

        public long SourceId { get; set; }
        public long TargetId { get; set; }
    }
}