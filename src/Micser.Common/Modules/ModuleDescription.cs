using Micser.Common.Widgets;

namespace Micser.Common.Modules
{
    public class ModuleDescription : Model
    {
        public ModuleState ModuleState { get; set; }
        public string ModuleType { get; set; }
        public WidgetState WidgetState { get; set; }
        public string WidgetType { get; set; }
    }
}