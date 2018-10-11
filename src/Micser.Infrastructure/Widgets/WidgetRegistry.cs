using System.Collections.Generic;
using Unity;

namespace Micser.Infrastructure.Widgets
{
    public class WidgetRegistry : IWidgetRegistry
    {
        private readonly IUnityContainer _container;

        public WidgetRegistry(IUnityContainer container)
        {
            _container = container;
        }

        public IEnumerable<WidgetDescription> Widgets => _container.Resolve<IEnumerable<WidgetDescription>>();
    }
}