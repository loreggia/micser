using System.Collections.Generic;
using Prism.Modularity;

namespace Micser.Infrastructure
{
    public interface IModuleWithResources : IModule
    {
        IEnumerable<string> ResourcePaths { get; }
    }
}