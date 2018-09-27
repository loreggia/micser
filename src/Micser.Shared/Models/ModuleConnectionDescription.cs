namespace Micser.Shared.Models
{
    public class ModuleConnectionDescription : Model
    {
        public ModuleConnectionDescription()
        {
        }

        public ModuleConnectionDescription(int sourceId, int targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
        }

        public int SourceId { get; set; }
        public int TargetId { get; set; }
    }
}