namespace Micser.Shared.Models
{
    public class ModuleConnection : Model
    {
        public ModuleConnection()
        {
        }

        public ModuleConnection(int sourceId, int targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
        }

        public int SourceId { get; set; }
        public int TargetId { get; set; }
    }
}