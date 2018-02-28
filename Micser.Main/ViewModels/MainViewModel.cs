using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Micser.Infrastructure;
using Micser.Infrastructure.Widgets;
using Micser.Main.ViewModels.Widgets;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        public const string WidgetsConfigurationKey = "Widgets";

        private readonly IConfigurationService _configurationService;
        private readonly IList<ConnectionViewModel> _connections;
        private readonly IList<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;

        public MainViewModel(IConfigurationService configurationService, IWidgetFactory widgetFactory, IEnumerable<WidgetDescription> availableWidgets)
        {
            _configurationService = configurationService;
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

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);

            var widgetStates = new List<WidgetState>();

            foreach (var vm in Widgets)
            {
            }

            _configurationService.SetSetting(WidgetsConfigurationKey, );
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var widgetStates = _configurationService.GetSetting<IEnumerable<WidgetState>>(WidgetsConfigurationKey);

            var input = new DeviceInputViewModel { Position = new Point(10, 10) };
            var output = new DeviceOutputViewModel { Position = new Point(10, 100) };

            var source = input.OutputConnectors.First();
            var sink = output.InputConnectors.First();

            _widgets.Add(input);
            _widgets.Add(output);
            _connections.Add(new ConnectionViewModel { Source = source, Sink = sink });
        }
    }
}