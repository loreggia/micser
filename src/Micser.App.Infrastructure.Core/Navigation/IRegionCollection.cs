using System.Collections.Generic;

namespace Micser.App.Infrastructure.Navigation
{
    public interface IRegionCollection : IEnumerable<IRegion>
    {
        IRegion this[string name] { get; }

        void Add(IRegion region);

        IRegion GetByName(string name);
    }
}