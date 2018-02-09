using Micser.Infrastructure.Controls;

namespace Micser.Main.Views.Widgets
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