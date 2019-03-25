﻿using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Widgets;
using Micser.Common.Modules;
using NLog;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Micser.App.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly ModuleConnectionsApiClient _connectionsApiClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ModulesApiClient _modulesApiClient;
        private readonly INavigationManager _navigationManager;
        private readonly ICollection<WidgetViewModel> _savingBuffer;
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly ObservableCollection<WidgetViewModel> _widgets;
        private SubscriptionToken _apiEventSubscription;
        private IEnumerable<WidgetDescription> _availableWidgets;
        private bool _isLoaded;
        private bool _isLoading;

        public MainViewModel(
            IWidgetFactory widgetFactory,
            IWidgetRegistry widgetRegistry,
            ILogger logger,
            INavigationManager navigationManager,
            IEventAggregator eventAggregator,
            ModulesApiClient modulesApiClient,
            ModuleConnectionsApiClient connectionsApiClient)
        {
            _widgetRegistry = widgetRegistry;
            _logger = logger;
            _navigationManager = navigationManager;
            _eventAggregator = eventAggregator;
            _modulesApiClient = modulesApiClient;
            _connectionsApiClient = connectionsApiClient;

            _widgets = new ObservableCollection<WidgetViewModel>();
            _widgets.CollectionChanged += OnWidgetsCollectionChanged;
            _connections = new ObservableCollection<ConnectionViewModel>();
            _connections.CollectionChanged += OnConnectionsCollectionChanged;

            _savingBuffer = new List<WidgetViewModel>();

            WidgetFactory = widgetFactory;

            RefreshCommand = new DelegateCommand(LoadData, () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Refresh, RefreshCommand);

            DeleteCommand = new DelegateCommand(Delete, () => !IsBusy && Widgets.Any(w => w.IsSelected));
            AddCommandBinding(CustomApplicationCommands.Delete, DeleteCommand);
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
        }

        public IEnumerable<ConnectionViewModel> Connections => _connections;

        public ICommand DeleteCommand { get; }

        public ICommand RefreshCommand { get; }

        public IWidgetFactory WidgetFactory { get; }

        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        protected override void OnNavigatedFrom(object parameter)
        {
            _apiEventSubscription.Dispose();

            base.OnNavigatedFrom(parameter);
        }

        protected override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            _apiEventSubscription = _eventAggregator.GetEvent<ApiEvent>().Subscribe(OnApiEvent);

            if (_isLoaded)
            {
                return;
            }

            _navigationManager.ClearJournal(AppGlobals.PrismRegions.Main);

            LoadData();
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
                viewModel.SourceChanged += OnConnectionSourceChanged;
                viewModel.TargetChanged += OnConnectionTargetChanged;
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
                State = viewModel.GetState()
            };
            var result = await _modulesApiClient.CreateAsync(moduleDto);

            if (result.IsSuccess)
            {
                viewModel.Id = result.Data.Id;
                if (moduleDto.State != null)
                {
                    viewModel.LoadState(moduleDto.State);
                }
                viewModel.PropertyChanged += OnWidgetPropertyChanged;
            }
            else
            {
                // TODO error handling / remove it
            }
        }

        private void Delete()
        {
            var selectedWidgets = Widgets.Where(w => w.IsSelected).ToArray();
            foreach (var widget in selectedWidgets)
            {
                var connections = Connections.Where(c => c.Source.Widget == widget || c.Target.Widget == widget).ToArray();
                foreach (var connection in connections)
                {
                    _connections.Remove(connection);
                }
                _widgets.Remove(widget);
            }
        }

        private async void DeleteConnection(ConnectionViewModel viewModel)
        {
            var result = await _connectionsApiClient.DeleteAsync(viewModel.Id);

            if (!result.IsSuccess)
            {
                // TODO error handling
            }

            viewModel.SourceChanged -= OnConnectionSourceChanged;
            viewModel.TargetChanged -= OnConnectionTargetChanged;
        }

        private async void DeleteModule(WidgetViewModel viewModel)
        {
            var result = await _modulesApiClient.DeleteAsync(viewModel.Id);

            if (!result.IsSuccess)
            {
                // TODO error handling / reload everything
            }

            viewModel.PropertyChanged -= OnWidgetPropertyChanged;
        }

        private async Task LoadConnections()
        {
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
                                cvm.SourceChanged += OnConnectionSourceChanged;
                                cvm.TargetChanged += OnConnectionTargetChanged;
                            }
                            else
                            {
                                _logger.Warn($"Could not load widget connection. ID: {connectionDto.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Could not load widget connection. ID: {0}", connectionDto.Id);
                        }
                    }
                }
            }
        }

        private async void LoadData()
        {
            try
            {
                IsBusy = true;
                _isLoading = true;
                AvailableWidgets = _widgetRegistry.Widgets;

                if (_isLoaded)
                {
                    UnloadData();
                }

                await LoadWidgets();
                await Task.Delay(50);
                await LoadConnections();
            }
            finally
            {
                IsBusy = false;
                _isLoading = false;
                _isLoaded = true;
            }
        }

        private async Task LoadWidgets()
        {
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
                            vm.LoadState(module.State);
                            vm.PropertyChanged += OnWidgetPropertyChanged;
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
        }

        private void OnApiEvent(ApiEvent.ApiData data)
        {
            if (data.Action == "updatevolume" && data.Content is ModuleDto moduleDto && moduleDto.State != null)
            {
                if (Widgets.FirstOrDefault(w => w.Id == moduleDto.Id) is AudioWidgetViewModel widget)
                {
                    try
                    {
                        _isLoading = true;
                        widget.UseSystemVolume = moduleDto.State.UseSystemVolume;
                        widget.Volume = moduleDto.State.Volume;
                        widget.IsMuted = moduleDto.State.IsMuted;
                    }
                    finally
                    {
                        _isLoading = false;
                    }
                }
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

        private void OnConnectionSourceChanged(object sender, ConnectorChangedEventArgs e)
        {
            if (sender is ConnectionViewModel vm)
            {
                UpdateConnection(vm);
            }
        }

        private void OnConnectionTargetChanged(object sender, ConnectorChangedEventArgs e)
        {
            if (sender is ConnectionViewModel vm)
            {
                UpdateConnection(vm);
            }
        }

        private async void OnWidgetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isLoading)
            {
                return;
            }

            if (sender is WidgetViewModel vm)
            {
                if (_savingBuffer.Contains(vm))
                {
                    return;
                }

                _savingBuffer.Add(vm);
                await Task.Delay(50);
                _savingBuffer.Remove(vm);

                var moduleDto = new ModuleDto
                {
                    Id = vm.Id,
                    State = vm.GetState()
                };
                var result = await _modulesApiClient.UpdateAsync(moduleDto);

                if (!result.IsSuccess)
                {
                    // TODO error handling
                }
                else
                {
                }
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

        private void UnloadData()
        {
            var isLoading = _isLoading;
            _isLoading = true;

            _connections.Clear();
            _widgets.Clear();

            _isLoading = isLoading;
        }

        private async void UpdateConnection(ConnectionViewModel viewModel)
        {
            var connectionDto = new ModuleConnectionDto
            {
                Id = viewModel.Id,
                SourceConnectorName = viewModel.Source.Name,
                SourceId = viewModel.Source.Widget.Id,
                TargetConnectorName = viewModel.Target.Name,
                TargetId = viewModel.Target.Widget.Id
            };
            var result = await _connectionsApiClient.UpdateAsync(connectionDto);

            if (result.IsSuccess)
            {
                viewModel.Id = result.Data.Id;
            }
            else
            {
                // TODO error handling
            }
        }
    }
}