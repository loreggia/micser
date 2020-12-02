using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Micser.App.Infrastructure.Widgets
{
    /// <inheritdoc cref="IWidgetRegistry"/>
    public class WidgetRegistry : IWidgetRegistry
    {
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public WidgetRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IEnumerable<WidgetDescription> Widgets => _serviceProvider.GetRequiredService<IEnumerable<WidgetDescription>>();
    }
}