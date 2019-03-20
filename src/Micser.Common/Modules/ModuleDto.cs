namespace Micser.Common.Modules
{
    public class ModuleDto : IIdentifiable
    {
        public long Id { get; set; }
        public string ModuleType { get; set; }
        public ModuleState State { get; set; }
        public string WidgetType { get; set; }
    }
}