using System.Windows;
using System.Windows.Controls;

namespace Micser.Infrastructure.Controls
{
    public class BusyPanel : Control
    {
        static BusyPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyPanel), new FrameworkPropertyMetadata(typeof(BusyPanel)));
        }
    }
}