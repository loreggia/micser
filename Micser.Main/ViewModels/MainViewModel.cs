using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Micser.Infrastructure;
using Micser.Main.ViewModels.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        private readonly IList<ConnectionViewModel> _connections;
        private readonly IList<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;

        public MainViewModel(IWidgetFactory widgetFactory, IEnumerable<WidgetDescription> availableWidgets)
        {
            _widgets = new ObservableCollection<WidgetViewModel>();
            _connections = new ObservableCollection<ConnectionViewModel>();

            WidgetFactory = widgetFactory;
            AvailableWidgets = availableWidgets;
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
        }

        public IEnumerable<ConnectionViewModel> Connections => _connections;
        public IWidgetFactory WidgetFactory { get; }
        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var input = new DeviceInputViewModel();
            var output = new DeviceOutputViewModel();

            var source = input.OutputConnectors.First();
            var sink = output.InputConnectors.First();

            _widgets.Add(input);
            _widgets.Add(output);
            _connections.Add(new ConnectionViewModel { Source = source, Sink = sink });
        }
    }
}