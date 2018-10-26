namespace Micser.Infrastructure.Models
{
    public class ModuleDescription : Model
    {
        public ModuleState State { get; set; }
        public string Type { get; set; }
        public ModuleViewState ViewState { get; set; }
        public string ViewType { get; set; }
    }
}