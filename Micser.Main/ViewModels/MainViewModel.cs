using System.Collections.Generic;
using System.Collections.ObjectModel;
using Micser.Infrastructure;
using Micser.Main.ViewModels.Widgets;
using Micser.Main.Views.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private readonly IList<WidgetViewModel> _widgets;

        public MainViewModel(IWidgetFactory widgetFactory)
        {
            WidgetFactory = widgetFactory;
            _widgets = new ObservableCollection<WidgetViewModel>();
        }

        public IWidgetFactory WidgetFactory { get; }
        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            _widgets.Add(new DeviceInputViewModel());
            _widgets.Add(new DeviceOutputViewModel());
        }
    }
}