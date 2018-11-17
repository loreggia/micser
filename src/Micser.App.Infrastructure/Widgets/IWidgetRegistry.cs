using System.Collections.Generic;

namespace Micser.App.Infrastructure.Widgets
{
    public interface IWidgetRegistry
    {
        IEnumerable<WidgetDescription> Widgets { get; }
    }
}