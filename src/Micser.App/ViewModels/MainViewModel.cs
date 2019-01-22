using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Widgets;
using Micser.Common.DataAccess;
using NLog;
using Prism.Regions;

namespace Micser.App.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        public const string ConnectionsConfigurationKey = "Connections";
        public const string WidgetsConfigurationKey = "Widgets";

        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly IDatabase _database;
        private readonly ILogger _logger;
        private readonly ModulesApiClient _modulesApiClient;
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly ObservableCollection<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;

        public MainViewModel(IDatabase database, IWidgetFactory widgetFactory, IWidgetRegistry widgetRegistry, ILogger logger,
            ModulesApiClient modulesApiClient)
        {
            _database = database;
            _widgetRegistry = widgetRegistry;
            _logger = logger;
            _modulesApiClient = modulesApiClient;
            _widgets = new ObservableCollection<WidgetViewModel>();
            _widgets.CollectionChanged += OnWidgetsCollectionChanged;
            _connections = new ObservableCollection<ConnectionViewModel>();
            _connections.CollectionChanged += OnConnectionsCollectionChanged;

            WidgetFactory = widgetFactory;
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
                var state = new WidgetState
                {
                    Position = vm.Position,
                    Size = vm.Size
                };
                vm.SaveState(state);
                widgetStates.Add(state);
            }

            //_configurationService.SetSetting(WidgetsConfigurationKey, widgetStates);

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

            //_configurationService.SetSetting(ConnectionsConfigurationKey, connections);
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            AvailableWidgets = _widgetRegistry.Widgets;

            var modulesResult = await _modulesApiClient.GetAll();

            if (modulesResult.IsSuccess)
            {
                var modules = modulesResult.Data;

                if (modules != null)
                {
                    foreach (var module in modules)
                    {
                    }
                }
            }
            else
            {
                _logger.Error(modulesResult);
            }

            //var widgetStates = _configurationService.GetSettingEnumerable<WidgetState>(WidgetsConfigurationKey);

            //if (widgetStates != null)
            //{
            //    foreach (var widgetState in widgetStates)
            //    {
            //        //var vm = WidgetFactory.CreateViewModel(widgetState.ViewModelType);
            //        //vm.Position = widgetState.Position;
            //        //vm.Size = widgetState.Size;
            //        //vm.LoadState(widgetState);
            //        //_widgets.Add(vm);
            //    }
            //}

            //var connections = _configurationService.GetSettingEnumerable<ConnectionInfo>(ConnectionsConfigurationKey);

            //if (connections != null)
            //{
            //    foreach (var connectionInfo in connections)
            //    {
            //        var sourceWidget = Widgets.FirstOrDefault(w => w.Id == connectionInfo.SourceWidgetId);
            //        var sinkWidget = Widgets.FirstOrDefault(w => w.Id == connectionInfo.SinkWidgetId);

            //        var source = sourceWidget?.OutputConnectors.FirstOrDefault(c => c.Name == connectionInfo.SourceConnectorName);
            //        var sink = sinkWidget?.InputConnectors.FirstOrDefault(c => c.Name == connectionInfo.SinkConnectorName);

            //        if (source != null && sink != null)
            //        {
            //            var cvm = new ConnectionViewModel
            //            {
            //                Source = source,
            //                Sink = sink
            //            };
            //            _connections.Add(cvm);
            //            source.Connection = cvm;
            //            sink.Connection = cvm;
            //        }
            //    }
            //}
        }

        private void OnConnectionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        private void OnWidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (WidgetViewModel viewModel in e.NewItems)
                    {
                        CreateModule(viewModel);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (WidgetViewModel viewModel in e.OldItems)
                    {
                        DeleteModule(viewModel);
                    }

                    break;
            }
        }

        private void DeleteModule(WidgetViewModel viewModel)
        {
        }

        private void CreateModule(WidgetViewModel viewModel)
        {
        }
    }
}