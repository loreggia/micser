using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Widgets;
using Micser.Common.DataAccess;
using Micser.Common.Modules;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Micser.App.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        public const string ConnectionsConfigurationKey = "Connections";
        public const string WidgetsConfigurationKey = "Widgets";

        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly ModuleConnectionsApiClient _connectionsApiClient;
        private readonly IUnitOfWorkFactory _database;
        private readonly ILogger _logger;
        private readonly ModulesApiClient _modulesApiClient;
        private readonly INavigationManager _navigationManager;
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly ObservableCollection<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;
        private bool _isLoaded;
        private bool _isLoading;

        public MainViewModel(IUnitOfWorkFactory database, IWidgetFactory widgetFactory, IWidgetRegistry widgetRegistry, ILogger logger, INavigationManager navigationManager)
        {
            _database = database;
            _widgetRegistry = widgetRegistry;
            _logger = logger;
            _navigationManager = navigationManager;

            _modulesApiClient = new ModulesApiClient();
            _connectionsApiClient = new ModuleConnectionsApiClient();

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

        protected override void OnNavigatedFrom(object parameter)
        {
            base.OnNavigatedFrom(parameter);

            //var widgetStates = new List<WidgetState>();

            //foreach (var vm in Widgets)
            //{
            //    var state = new WidgetState
            //    {
            //        Position = vm.Position,
            //        Size = vm.Size
            //    };
            //    vm.SaveState(state);
            //    widgetStates.Add(state);
            //}

            //_configurationService.SetSetting(WidgetsConfigurationKey, widgetStates);

            //var connections = new List<ConnectionInfo>();
            //foreach (var connection in Connections)
            //{
            //    connections.Add(new ConnectionInfo
            //    {
            //        SourceWidgetId = connection.Source.Widget.Id,
            //        SourceConnectorName = connection.Source.Name,
            //        SinkWidgetId = connection.Sink.Widget.Id,
            //        SinkConnectorName = connection.Sink.Name
            //    });
            //}

            //_configurationService.SetSetting(ConnectionsConfigurationKey, connections);
        }

        protected override async void OnNavigatedTo(object parameter)
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoading = true;

            base.OnNavigatedTo(parameter);

            _navigationManager.ClearJournal(AppGlobals.PrismRegions.Main);

            AvailableWidgets = _widgetRegistry.Widgets;

            // widgets/modules
            var modulesResult = await _modulesApiClient.GetAllAsync();

            if (modulesResult.IsSuccess)
            {
                var modules = modulesResult.Data;

                if (modules != null)
                {
                    foreach (var module in modules)
                    {
                        try
                        {
                            var type = Type.GetType(module.WidgetType);
                            var vm = WidgetFactory.CreateViewModel(type);
                            vm.Id = module.Id;
                            vm.LoadState(module.WidgetState);
                            _widgets.Add(vm);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Could not load widget. ID: {0}", module.Id);
                        }
                    }
                }
            }
            else
            {
                _logger.Error(modulesResult);
            }

            // connections
            var connectionsResult = await _connectionsApiClient.GetAllAsync();

            if (connectionsResult.IsSuccess)
            {
                var connections = connectionsResult.Data;

                if (connections != null)
                {
                    foreach (var connectionDto in connections)
                    {
                        try
                        {
                            var sourceWidget = Widgets.FirstOrDefault(w => w.Id == connectionDto.SourceId);
                            var targetWidget = Widgets.FirstOrDefault(w => w.Id == connectionDto.TargetId);

                            var source = sourceWidget?.OutputConnectors.FirstOrDefault(c => c.Name == connectionDto.SourceConnectorName);
                            var target = targetWidget?.InputConnectors.FirstOrDefault(c => c.Name == connectionDto.TargetConnectorName);

                            if (source != null && target != null)
                            {
                                var cvm = new ConnectionViewModel
                                {
                                    Id = connectionDto.Id,
                                    Source = source,
                                    Target = target
                                };
                                _connections.Add(cvm);
                                source.Connection = cvm;
                                target.Connection = cvm;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Could not load widget connection. ID: {0}", connectionDto.Id);
                        }
                    }
                }
            }

            _isLoading = false;
            _isLoaded = true;
        }

        private async void CreateConnection(ConnectionViewModel viewModel)
        {
            var connectionDto = new ModuleConnectionDto
            {
                SourceConnectorName = viewModel.Source.Name,
                SourceId = viewModel.Source.Widget.Id,
                TargetConnectorName = viewModel.Target.Name,
                TargetId = viewModel.Target.Widget.Id
            };
            var result = await _connectionsApiClient.CreateAsync(connectionDto);

            if (result.IsSuccess)
            {
                viewModel.Id = result.Data.Id;
            }
            else
            {
                // TODO error handling
            }
        }

        private async void CreateModule(WidgetViewModel viewModel)
        {
            var moduleDto = new ModuleDto
            {
                ModuleType = viewModel.ModuleType.AssemblyQualifiedName,
                WidgetType = viewModel.GetType().AssemblyQualifiedName,
                WidgetState = viewModel.GetState()
            };
            var result = await _modulesApiClient.CreateAsync(moduleDto);

            if (result.IsSuccess)
            {
                viewModel.Id = result.Data.Id;

                if (moduleDto.WidgetState != null)
                {
                    viewModel.LoadState(moduleDto.WidgetState);
                }
            }
            else
            {
                // TODO error handling / remove it
            }
        }

        private async void DeleteConnection(ConnectionViewModel viewModel)
        {
            var result = await _connectionsApiClient.DeleteAsync(viewModel.Id);

            if (!result.IsSuccess)
            {
                // TODO error handling
            }
        }

        private async void DeleteModule(WidgetViewModel viewModel)
        {
            var result = await _modulesApiClient.DeleteAsync(viewModel.Id);

            if (!result.IsSuccess)
            {
                // TODO error handling / reload everything
            }
        }

        private void OnConnectionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (ConnectionViewModel viewModel in e.NewItems)
                    {
                        CreateConnection(viewModel);
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (ConnectionViewModel viewModel in e.OldItems)
                    {
                        DeleteConnection(viewModel);
                    }

                    break;
            }
        }

        private void OnWidgetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

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
    }
}