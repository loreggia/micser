using Micser.Main.Controls;

namespace Micser.Main.Views.Widgets
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
