using Micser.Infrastructure.Widgets;

namespace Micser.Plugins.Main.Views.Widgets
{
    public partial class DeviceOutputWidget
    {
        public DeviceOutputWidget()
        {
            InitializeComponent();
            SetResourceReference(StyleProperty, typeof(Widget));
        }
    }
}