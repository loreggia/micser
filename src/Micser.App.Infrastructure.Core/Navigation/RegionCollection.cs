using System;
using System.Collections;
using System.Collections.Generic;

namespace Micser.App.Infrastructure.Navigation
{
    public class RegionCollection : IRegionCollection
    {
        private readonly Dictionary<string, IRegion> _regions;

        public RegionCollection()
        {
            _regions = new Dictionary<string, IRegion>();
        }

        public IRegion this[string name] => GetByName(name);

        public void Add(IRegion region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            _regions[region.Name] = region;
        }

        public IRegion GetByName(string name)
        {
            return _regions.TryGetValue(name, out var region) ? region : null;
        }

        public IEnumerator<IRegion> GetEnumerator()
        {
            return _regions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}