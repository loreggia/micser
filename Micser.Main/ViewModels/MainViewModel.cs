using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Micser.Infrastructure;
using Micser.Infrastructure.Widgets;
using Micser.Main.Views;
using Prism.Regions;

namespace Micser.Main.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        public const string ConnectionsConfigurationKey = "Connections";
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

            if (navigationContext.Uri.OriginalString == typeof(MainView).Name)
            {
                // TODO why is this method called at startup
                return;
            }

            var widgetStates = new List<WidgetState>();

            foreach (var vm in Widgets)
            {
                var state = new WidgetState
                {
                    Id = vm.Id,
                    Name = vm.Name,
                    Position = vm.Position,
                    ViewModelType = vm.GetType()
                };
                vm.SaveState(state);
                widgetStates.Add(state);
            }

            _configurationService.SetSetting(WidgetsConfigurationKey, widgetStates);

            var connections = new List<ConnectionInfo>();
            foreach (var connection in Connections)
            {
                connections.Add(new ConnectionInfo
                {
                    SourceWidgetId = connection.Source.Widget.Id,
                    SourceConnectorName = connection.Source.Name,
                    SinkWidgetId = connection.Sink.Widget.Id,
                    SinkConnectorName = connection.Sink.Name
                });
            }
            _configurationService.SetSetting(ConnectionsConfigurationKey, connections);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var widgetStates = _configurationService.GetSettingEnumerable<WidgetState>(WidgetsConfigurationKey);

            if (widgetStates != null)
            {
                foreach (var widgetState in widgetStates)
                {
                    var vm = WidgetFactory.CreateViewModel(widgetState.ViewModelType);
                    vm.Id = widgetState.Id;
                    vm.Name = widgetState.Name;
                    vm.Position = widgetState.Position;
                    _widgets.Add(vm);
                }
            }

            var connections = _configurationService.GetSettingEnumerable<ConnectionInfo>(ConnectionsConfigurationKey);

            if (connections != null)
            {
                foreach (var connectionInfo in connections)
                {
                    var sourceWidget = Widgets.FirstOrDefault(w => w.Id == connectionInfo.SourceWidgetId);
                    var sinkWidget = Widgets.FirstOrDefault(w => w.Id == connectionInfo.SinkWidgetId);

                    var source = sourceWidget?.OutputConnectors.FirstOrDefault(c => c.Name == connectionInfo.SourceConnectorName);
                    var sink = sinkWidget?.InputConnectors.FirstOrDefault(c => c.Name == connectionInfo.SinkConnectorName);

                    if (source != null && sink != null)
                    {
                        var cvm = new ConnectionViewModel
                        {
                            Source = source,
                            Sink = sink
                        };
                        _connections.Add(cvm);
                        source.Connection = cvm;
                        sink.Connection = cvm;
                    }
                }
            }
        }
    }
}