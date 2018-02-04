using System.Collections.Generic;
using System.Collections.ObjectModel;
using Micser.Infrastructure;
using Micser.Main.ViewModels.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private readonly IList<WidgetViewModel> _widgets;

        public MainViewModel()
        {
            _widgets = new ObservableCollection<WidgetViewModel>();
        }

        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            _widgets.Add(new DeviceInputViewModel());
        }
    }
}