using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// A simple control that shows a loading spinner.
    /// </summary>
    public class BusyPanel : Control
    {
        static BusyPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyPanel), new FrameworkPropertyMetadata(typeof(BusyPanel)));
        }
    }
}