using Prism.Events;

namespace Micser.App.Infrastructure
{
    public class ApplicationEvents
    {
        public class ModulesLoaded : PubSubEvent
        {
        }

        public class StatusChange : PubSubEvent<string>
        {
        }
    }
}