using System.Collections.Generic;
using Unity;

namespace Micser.App.Infrastructure.Widgets
{
    /// <inheritdoc cref="IWidgetRegistry"/>
    public class WidgetRegistry : IWidgetRegistry
    {
        private readonly IUnityContainer _container;

        /// <inheritdoc />
        public WidgetRegistry(IUnityContainer container)
        {
            _container = container;
        }

        /// <inheritdoc />
        public IEnumerable<WidgetDescription> Widgets => _container.Resolve<IEnumerable<WidgetDescription>>();
    }
}