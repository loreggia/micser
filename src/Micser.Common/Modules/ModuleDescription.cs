namespace Micser.Common.Modules
{
    public class ModuleDescription : Model
    {
        public StateDictionary State { get; set; }
        public string Type { get; set; }
        public StateDictionary ViewState { get; set; }
    }
}