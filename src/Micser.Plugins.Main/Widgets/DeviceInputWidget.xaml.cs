using Micser.Infrastructure.Widgets;

namespace Micser.Plugins.Main.Views.Widgets
{
    public partial class DeviceInputWidget
    {
        public DeviceInputWidget()
        {
            InitializeComponent();
            SetResourceReference(StyleProperty, typeof(Widget));
        }
    }
}