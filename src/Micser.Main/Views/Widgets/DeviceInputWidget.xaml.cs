using Micser.Infrastructure.Controls;
using Micser.Infrastructure.Widgets;

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