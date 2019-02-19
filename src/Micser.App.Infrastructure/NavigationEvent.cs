using Prism.Events;

namespace Micser.App.Infrastructure
{
    public class NavigationEvent : PubSubEvent<NavigationInfo>
    {
    }

    public class NavigationInfo
    {
        public object Parameter { get; set; }
        public string RegionName { get; set; }
        public string ViewName { get; set; }
    }
}