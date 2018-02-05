using System.Windows;
using Micser.Main.Controls;

namespace Micser.Main.Views.Widgets
{
    public class DeviceInputWidget : Widget
    {
        static DeviceInputWidget()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DeviceInputWidget), new FrameworkPropertyMetadata(typeof(DeviceInputWidget)));
        }
    }
}