using Micser.Infrastructure.Controls;
using Micser.Infrastructure.Widgets;

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