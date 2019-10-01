using Micser.App.Infrastructure.Events;

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
        public class ModulesLoaded : Event<>
        {
        }

        /// <summary>
        /// An event that is fired after a navigation has occurred.
        /// </summary>
        public class Navigated : Event<Navigated.NavigationInfo>
        {
            /// <summary>
            /// The data passed in the <see cref="Navigated"/> event.
            /// </summary>
            public class NavigationInfo
            {
                /// <summary>
                /// Gets or sets the navigation parameter object.
                /// </summary>
                public object Parameter { get; set; }

                /// <summary>
                /// Gets or sets the name of the region where the navigation happened.
                /// </summary>
                public string RegionName { get; set; }

                /// <summary>
                /// Gets or sets the name of the view that was navigated to.
                /// </summary>
                public string ViewName { get; set; }
            }
        }

        /// <summary>
        /// An event containing <see cref="string"/> data to display in the main status bar.
        /// </summary>
        public class StatusChange : Event<string>
        {
        }
    }
}