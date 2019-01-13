namespace Micser.Common.Modules
{
    public class ModuleDescription : Model
    {
        public IModuleState State { get; set; }
        public string Type { get; set; }
        public IModuleViewState ViewState { get; set; }
    }
}