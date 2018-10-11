using System.Collections.Generic;

namespace Micser.Infrastructure.Widgets
{
    public interface IWidgetRegistry
    {
        IEnumerable<WidgetDescription> Widgets { get; }
    }
}