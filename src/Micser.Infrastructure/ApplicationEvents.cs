using Prism.Events;

namespace Micser.Infrastructure
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