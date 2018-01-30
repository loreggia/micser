using System.Windows;
using System.Windows.Controls;

namespace Micser.Main.Controls
{
    public class WidgetPanel : Control
    {
        static WidgetPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WidgetPanel), new FrameworkPropertyMetadata(typeof(WidgetPanel)));
        }
    }
}