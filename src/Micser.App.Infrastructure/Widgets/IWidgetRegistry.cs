using System.Collections.Generic;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Registry containing the widget descriptions of all loaded plugins.
    /// </summary>
    public interface IWidgetRegistry
    {
        /// <summary>
        /// Gets the descriptions for all available widgets.
        /// </summary>
        IEnumerable<WidgetDescription> Widgets { get; }
    }
}