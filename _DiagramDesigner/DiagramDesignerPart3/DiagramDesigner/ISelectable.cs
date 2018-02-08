
namespace DiagramDesigner
{
    // Common interface for items that can be selected
    // on the WidgetPanel; used by Widget and Connection
    public interface ISelectable
    {
        bool IsSelected { get; set; }
    }
}
