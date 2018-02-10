using Micser.Infrastructure.Controls;

namespace Micser.Infrastructure
{
    public interface IWidgetFactory
    {
        Widget CreateWidget(WidgetDescription description);
    }
}