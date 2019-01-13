using System;

namespace Micser.Common.Modules
{
    public class ModuleConnectionDescription : Model
    {
        public ModuleConnectionDescription()
        {
        }

        public ModuleConnectionDescription(Guid sourceId, Guid targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
        }

        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }
    }
}