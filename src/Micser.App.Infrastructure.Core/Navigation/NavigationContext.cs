using System;

namespace Micser.App.Infrastructure.Navigation
{
    public class NavigationContext
    {
        public NavigationContext(IRegion region, Uri uri, object parameter)
        {
            Region = region;
            Uri = uri;
            Parameter = parameter;
        }

        public object Parameter { get; }
        public IRegion Region { get; }
        public Uri Uri { get; }
    }
}