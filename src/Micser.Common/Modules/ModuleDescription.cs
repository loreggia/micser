using System;

namespace Micser.Common.Modules
{
    public class ModuleDefinition
    {
        public ModuleDefinition(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }
    }
}