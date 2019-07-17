using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Widgets;
using Micser.App.Properties;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Common.Settings;
using NLog;
using Prism.Commands;
using Prism.Events;
using Prism.Interactivity.InteractionRequest;
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
        private readonly ModulesSerializer _modulesSerializer;
        private readonly INavigationManager _navigationManager;
        private readonly ICollection<WidgetViewModel> _savingBuffer;
        private readonly ISettingsService _settingsService;
        private readonly IWidgetRegistry _widgetRegistry;
        private readonly ObservableCollection<WidgetViewModel> _widgets;
        private SubscriptionToken _apiEventSubscription;
        private IEnumerable<WidgetDescription> _availableWidgets;
        private IEnumerable<ModuleConnectionDto> _connectionDtos;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isWidgetToolBoxOpen;
        private IEnumerable<ModuleDto> _moduleDtos;

        public MainViewModel(
            IWidgetFactory widgetFactory,
            IWidgetRegistry widgetRegistry,
            ILogger logger,
            INavigationManager navigationManager,
            IEventAggregator eventAggregator,
            ISettingsService settingsService,
            ModulesApiClient modulesApiClient,
            ModuleConnectionsApiClient connectionsApiClient,
            ModulesSerializer modulesSerializer)
        {
            _widgetRegistry = widgetRegistry;
            _logger = logger;
            _navigationManager = navigationManager;
            _eventAggregator = eventAggregator;
            _settingsService = settingsService;
            _modulesApiClient = modulesApiClient;
            _connectionsApiClient = connectionsApiClient;
            _modulesSerializer = modulesSerializer;

            _widgets = new ObservableCollection<WidgetViewModel>();
            _widgets.CollectionChanged += OnWidgetsCollectionChanged;
            _connections = new ObservableCollection<ConnectionViewModel>();
            _connections.CollectionChanged += OnConnectionsCollectionChanged;

            _savingBuffer = new List<WidgetViewModel>();

            WidgetFactory = widgetFactory;

            RefreshCommand = new DelegateCommand(async () => await LoadDataAsync(), () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Refresh, RefreshCommand);

            DeleteCommand = new DelegateCommand(Delete, () => !IsBusy && Widgets.Any(w => w.IsSelected));
            AddCommandBinding(CustomApplicationCommands.Delete, DeleteCommand);

            ImportFileCommand = new DelegateCommand(ImportFile, () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Import, ImportFileCommand);
            ImportFileRequest = new InteractionRequest<IConfirmation>();

            ExportFileCommand = new DelegateCommand(ExportFile, () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Export, ExportFileCommand);
            ExportFileRequest = new InteractionRequest<IConfirmation>();
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
        }

        public IEnumerable<ConnectionViewModel> Connections => _connections;

        public ICommand DeleteCommand { get; }

        public ICommand ExportFileCommand { get; }

        public InteractionRequest<IConfirmation> ExportFileRequest { get; set; }

        public ICommand ImportFileCommand { get; }

        public InteractionRequest<IConfirmation> ImportFileRequest { get; set; }

        public bool IsWidgetToolBoxOpen
        {
            get => _isWidgetToolBoxOpen;
            set
            {
                if (SetProperty(ref _isWidgetToolBoxOpen, value))
                {
                    _settingsService.SetSettingAsync(AppGlobals.SettingKeys.IsWidgetToolBoxOpen, value);
                }
            }
        }

        public ICommand RefreshCommand { get; }

        public IWidgetFactory WidgetFactory { get; }

        public IEnumerable<WidgetViewModel> Widgets => _widgets;

        protected override void OnNavigatedFrom(object parameter)
        {
            _apiEventSubscription.Dispose();

            base.OnNavigatedFrom(parameter);
        }

        protected override async void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            _apiEventSubscription = _eventAggregator.GetEvent<ApiEvent>().Subscribe(OnApiEvent);

            IsWidgetToolBoxOpen = _settingsService.GetSetting<bool>(AppGlobals.SettingKeys.IsWidgetToolBoxOpen);

            if (_isLoaded)
            {
                return;
            }

            _navigationManager.ClearJournal(AppGlobals.PrismRegions.Main);

            await LoadDataAsync();
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

        private void ExportFile()
        {
            var confirmation = new FileDialogConfirmation { Title = Resources.ExportConfigurationDialogTitle, DefaultExtension = ".json" };
            confirmation.AddFilter(Resources.JsonFiles, "*.json");
            ExportFileRequest.Raise(confirmation, async c =>
            {
                if (!c.Confirmed)
                {
                    return;
                }

                if (c.Content is string fileName)
                {
                    await LoadDataAsync();

                    var data = new ModulesExportDto
                    {
                        Modules = _moduleDtos.ToArray(),
                        Connections = _connectionDtos.ToArray()
                    };

                    _modulesSerializer.Export(fileName, data);
                }
            });
        }

        private void ImportFile()
        {
            var confirmation = new FileDialogConfirmation { Title = Resources.ImportConfigurationDialogTitle, DefaultExtension = ".json" };
            confirmation.AddFilter(Resources.JsonFiles, "*.json");
            ImportFileRequest.Raise(confirmation, async c =>
            {
                if (!c.Confirmed)
                {
                    return;
                }

                if (c.Content is string fileName)
                {
                    var dto = _modulesSerializer.Import(fileName);

                    if (dto == null)
                    {
                        _logger.Error("Import data is empty.");
                        return;
                    }

                    if (await _modulesApiClient.ImportConfigurationAsync(dto))
                    {
                        _logger.Warn("Import failed. See engine log for details.");
                    }

                    await LoadDataAsync();
                }
            });
        }

        private async Task LoadConnections()
        {
            var connectionsResult = await _connectionsApiClient.GetAllAsync();

            if (connectionsResult.IsSuccess)
            {
                _connectionDtos = connectionsResult.Data;

                if (_connectionDtos != null)
                {
                    foreach (var connectionDto in _connectionDtos)
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

        private async Task LoadDataAsync()
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

                await LoadWidgetsAsync();
                await LoadConnections();
            }
            finally
            {
                IsBusy = false;
                _isLoading = false;
                _isLoaded = true;
            }
        }

        private async Task LoadWidgetsAsync()
        {
            var modulesResult = await _modulesApiClient.GetAllAsync();

            if (modulesResult.IsSuccess)
            {
                _moduleDtos = modulesResult.Data;

                if (_moduleDtos != null)
                {
                    foreach (var module in _moduleDtos)
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

                if (vm.GetType().GetProperty(e.PropertyName)?.GetCustomAttributes(true).OfType<UnsavedAttribute>().Any() == true)
                {
                    return;
                }

                _savingBuffer.Add(vm);
                await Task.Delay(50);

                lock (_savingBuffer)
                {
                    if (!_savingBuffer.Contains(vm))
                    {
                        return;
                    }

                    _savingBuffer.Remove(vm);
                }

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