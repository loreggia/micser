using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Widgets;
using Micser.Common.DataAccess;
using Micser.Common.Modules;
using Micser.Common.Widgets;
using NLog;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Micser.App.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        public const string ConnectionsConfigurationKey = "Connections";
        public const string WidgetsConfigurationKey = "Widgets";

        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly IUnitOfWorkFactory _database;
        private readonly ILogger _logger;
        private readonly ModulesApiClient _modulesApiClient;
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly ObservableCollection<WidgetViewModel> _widgets;
        private IEnumerable<WidgetDescription> _availableWidgets;
        private bool _isLoading;

        public MainViewModel(IUnitOfWorkFactory database, IWidgetFactory widgetFactory, IWidgetRegistry widgetRegistry, ILogger logger,
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

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            _isLoading = true;

            base.OnNavigatedTo(navigationContext);

            AvailableWidgets = _widgetRegistry.Widgets;

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

            _isLoading = false;

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

        private async void CreateModule(WidgetViewModel viewModel)
        {
            var moduleDescription = new ModuleDescription
            {
                ModuleType = viewModel.ModuleType.AssemblyQualifiedName,
                WidgetType = viewModel.GetType().AssemblyQualifiedName,
                WidgetState = viewModel.GetState()
            };
            var result = await _modulesApiClient.CreateAsync(moduleDescription);

            if (result.IsSuccess)
            {
                viewModel.Id = result.Data.Id;

                if (moduleDescription.WidgetState is WidgetState widgetState)
                {
                    viewModel.LoadState(widgetState);
                }
            }
            else
            {
                // TODO error handling / remove it
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