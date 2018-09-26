namespace Micser.Shared.Models
{
    public class ModuleConnectionDescription : Model
    {
        public ModuleDescription Source { get; set; }
        public ModuleDescription Target { get; set; }
    }
}