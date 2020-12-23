namespace Micser.Common.Modules
{
    public class ModuleDescription
    {
        public ModuleDescription(string name, string title, string description)
        {
            Name = name;
            Title = title;
            Description = description;
        }

        public string Description { get; }
        public string Name { get; }
        public string Title { get; }
    }
}