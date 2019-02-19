using Prism.Events;

namespace Micser.App.Infrastructure
{
    public class ApplicationEvents
    {
        public class ModulesLoaded : PubSubEvent
        {
        }

        public class Navigated : PubSubEvent<Navigated.NavigationInfo>
        {
            public class NavigationInfo
            {
                public object Parameter { get; set; }
                public string RegionName { get; set; }
                public string ViewName { get; set; }
            }
        }

        public class StatusChange : PubSubEvent<string>
        {
        }
    }
}