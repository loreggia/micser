namespace Micser.Models
{
    public class PluginDescriptionDto
    {
        public PluginDescriptionDto(string assemblyName, string moduleName)
        {
            AssemblyName = assemblyName;
            ModuleName = moduleName;
        }

        public string AssemblyName { get; }
        public string ModuleName { get; }
    }
}