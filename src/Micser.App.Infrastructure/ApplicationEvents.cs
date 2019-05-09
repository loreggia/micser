using Prism.Events;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Contains global application event classes that can be accessed using the <see cref="IEventAggregator"/> interface.
    /// </summary>
    public class ApplicationEvents
    {
        /// <summary>
        /// An event that is fired after all available modules have finished loading.
        /// </summary>
        public class ModulesLoaded : PubSubEvent
        {
        }

        /// <summary>
        /// An event that is fired after a navigation has occurred.
        /// </summary>
        public class Navigated : PubSubEvent<Navigated.NavigationInfo>
        {
            public class NavigationInfo
            {
                public object Parameter { get; set; }
                public string RegionName { get; set; }
                public string ViewName { get; set; }
            }
        }

        /// <summary>
        /// An event containing <see cref="string"/> data to display in the main status bar.
        /// </summary>
        public class StatusChange : PubSubEvent<string>
        {
        }
    }
}