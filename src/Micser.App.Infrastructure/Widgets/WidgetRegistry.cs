using Micser.Common;
using Micser.Common.Extensions;
using System.Collections.Generic;

namespace Micser.App.Infrastructure.Widgets
{
    /// <inheritdoc cref="IWidgetRegistry"/>
    public class WidgetRegistry : IWidgetRegistry
    {
        private readonly IContainerProvider _container;

        /// <inheritdoc />
        public WidgetRegistry(IContainerProvider container)
        {
            _container = container;
        }

        /// <inheritdoc />
        public IEnumerable<WidgetDescription> Widgets => _container.ResolveAll<WidgetDescription>();
    }
}