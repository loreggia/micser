using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Api;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Navigation;
using Micser.App.Infrastructure.Widgets;
using Micser.App.Resources;
using Micser.Common.Api;
using Micser.Common.Api.Extensions;
using Micser.Common.Extensions;
using Micser.Common.Modules;
using Micser.Common.Settings;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;

namespace Micser.App.ViewModels
{
    public class MainViewModel : ViewModelNavigationAware
    {
        private readonly ObservableCollection<ConnectionViewModel> _connections;
        private readonly ModuleConnectionsApiClient _connectionsApiClient;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<MainViewModel> _logger;
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
            ILogger<MainViewModel> logger,
            INavigationManager navigationManager,
            IEventAggregator eventAggregator,
            ISettingsService settingsService,
            IDialogService dialogService,
            ModulesApiClient modulesApiClient,
            ModuleConnectionsApiClient connectionsApiClient,
            ModulesSerializer modulesSerializer)
        {
            _widgetRegistry = widgetRegistry;
            _logger = logger;
            _navigationManager = navigationManager;
            _eventAggregator = eventAggregator;
            _settingsService = settingsService;
            _dialogService = dialogService;
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

            ExportFileCommand = new DelegateCommand(ExportFile, () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Export, ExportFileCommand);
        }

        public IEnumerable<WidgetDescription> AvailableWidgets
        {
            get => _availableWidgets;
            set => SetProperty(ref _availableWidgets, value);
        }

        public IEnumerable<ConnectionViewModel> Connections => _connections;

        public ICommand DeleteCommand { get; }

        public ICommand ExportFileCommand { get; }

        public ICommand ImportFileCommand { get; }

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
            var connection = new ModuleConnection
            {
                SourceConnectorName = viewModel.Source.Name,
                SourceId = viewModel.Source.Widget.Id,
                TargetConnectorName = viewModel.Target.Name,
                TargetId = viewModel.Target.Widget.Id
            };
            var result = await _connectionsApiClient.CreateAsync(connection);

            if (result != null)
            {
                viewModel.Id = result.Id;
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
            var module = new Module
            {
                ModuleType = viewModel.ModuleType.AssemblyQualifiedName,
                WidgetType = viewModel.GetType().AssemblyQualifiedName,
                State = { viewModel.GetState() }
            };
            var result = await _modulesApiClient.CreateAsync(module);

            if (result != null)
            {
                viewModel.Id = result.Id;
                if (module.State.Count > 0)
                {
                    viewModel.LoadState(module.AsDto().State);
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
            var result = await _connectionsApiClient.DeleteAsync(new Identifiable { Id = viewModel.Id });

            if (!result.Value)
            {
                // TODO error handling
            }

            viewModel.SourceChanged -= OnConnectionSourceChanged;
            viewModel.TargetChanged -= OnConnectionTargetChanged;
        }

        private async void DeleteModule(WidgetViewModel viewModel)
        {
            var result = await _modulesApiClient.DeleteAsync(new Identifiable { Id = viewModel.Id });

            if (!result.Value)
            {
                // TODO error handling / reload everything
            }

            viewModel.PropertyChanged -= OnWidgetPropertyChanged;
        }

        private async void ExportFile()
        {
            if (_dialogService.ShowSaveFileDialog(
                new FileDialogOptions(Strings.ExportConfigurationDialogTitle, ".json"),
                out var fileName))
            {
                await LoadDataAsync();

                var data = new ModulesExportDto
                {
                    Modules = _moduleDtos.ToArray(),
                    Connections = _connectionDtos.ToArray()
                };

                _modulesSerializer.Export(fileName, data);
            }
        }

        private async void ImportFile()
        {
            if (_dialogService.ShowOpenFileDialog(
                new FileDialogOptions(Strings.ImportConfigurationDialogTitle, ".json"),
                out var fileName))
            {
                var dto = _modulesSerializer.Import(fileName);

                if (dto == null)
                {
                    _logger.LogError("Import data is empty.");
                    return;
                }

                var request = new ImportRequest
                {
                    Modules = { dto.Modules.Select(x => x.AsApiModel()) },
                    Connections = { dto.Connections.Select(x => x.AsApiModel()) }
                };
                var result = await _modulesApiClient.ImportAsync(request);

                if (!result.Value)
                {
                    _logger.LogWarning("Import failed. See engine log for details.");
                }

                await LoadDataAsync();
            }
        }

        private async Task LoadConnections()
        {
            var connectionsResult = await _connectionsApiClient.GetAllAsync(new Empty());

            _connectionDtos = connectionsResult.Items.Select(x => x.AsDto()).ToArray();

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
                        _logger.LogWarning($"Could not load widget connection. ID: {connectionDto.Id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not load widget connection. ID: {0}", connectionDto.Id);
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
            var modulesResult = await _modulesApiClient.GetAllAsync(new Empty());

            _moduleDtos = modulesResult.Items.Select(x => x.AsDto()).ToArray();

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
                    _logger.LogError(ex, "Could not load widget. ID: {0}", module.Id);
                }
            }
        }

        private async void OnApiEvent(ApiEvent.ApiData data)
        {
            if (data.Action == "VolumeChanged")
            {
                if (data.Content is ModuleDto moduleDto &&
                    moduleDto.State != null &&
                    Widgets.FirstOrDefault(w => w.Id == moduleDto.Id) is AudioWidgetViewModel widget)
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
            else if (data.Action == "refresh")
            {
                if (data.Content is ModuleDto moduleDto && moduleDto.State != null)
                {
                    try
                    {
                        _isLoading = true;
                        Widgets.FirstOrDefault(w => w.Id == moduleDto.Id)?.LoadState(moduleDto.State);
                    }
                    finally
                    {
                        _isLoading = false;
                    }
                }
                else
                {
                    await LoadDataAsync();
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

                var moduleDto = new Module
                {
                    Id = vm.Id,
                    State = { vm.GetState() }
                };
                var result = await _modulesApiClient.UpdateAsync(moduleDto);

                if (result != null)
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
            var connectionDto = new ModuleConnection
            {
                Id = viewModel.Id,
                SourceConnectorName = viewModel.Source.Name,
                SourceId = viewModel.Source.Widget.Id,
                TargetConnectorName = viewModel.Target.Name,
                TargetId = viewModel.Target.Widget.Id
            };
            var result = await _connectionsApiClient.UpdateAsync(connectionDto);

            if (result != null)
            {
                viewModel.Id = result.Id;
            }
            else
            {
                // TODO error handling
            }
        }
    }
}