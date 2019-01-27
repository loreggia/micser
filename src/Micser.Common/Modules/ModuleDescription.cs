using Micser.Common.Widgets;

namespace Micser.Common.Modules
{
    public class ModuleDescription : Model
    {
        public ModuleState State { get; set; }
        public string Type { get; set; }
        public WidgetState ViewState { get; set; }
    }
}