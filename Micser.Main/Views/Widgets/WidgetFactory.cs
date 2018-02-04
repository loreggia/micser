using Micser.Main.Controls;
using Micser.Main.ViewModels.Widgets;

namespace Micser.Main.Views.Widgets
{
    public class WidgetFactory : IWidgetFactory
    {
        public virtual Widget CreateWidget(WidgetViewModel viewModel)
        {
            // todo generic approach?
            if (viewModel is DeviceInputViewModel)
            {
                return new DeviceInputWidget
                {
                    DataContext = viewModel
                };
            }

            return null;
        }
    }
}