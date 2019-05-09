using System.Collections.Generic;

namespace Micser.App.Infrastructure.Widgets
{
    /// <summary>
    /// Registry containing the widget descriptions of all loaded plugins.
    /// </summary>
    public interface IWidgetRegistry
    {
        IEnumerable<WidgetDescription> Widgets { get; }
    }
}