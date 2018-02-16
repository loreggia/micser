namespace Micser.Infrastructure.Widgets
{
    public interface IWidgetFactory
    {
        Widget CreateWidget(object dataContext);
    }
}