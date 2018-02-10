using System.Collections.Generic;
using System.Collections.ObjectModel;
using Micser.Infrastructure;
using Micser.Infrastructure.ViewModels;
using Micser.Main.ViewModels.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        private readonly IList<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;

        public MainViewModel(IWidgetFactory widgetFactory, IEnumerable<WidgetDescription> availableWidgets)
        {
            _widgets = new ObservableCollection<WidgetViewModel>();

            WidgetFactory = widgetFactory;
            AvailableWidgets = availableWidgets;
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
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