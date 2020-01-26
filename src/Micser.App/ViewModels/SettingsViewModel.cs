using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Settings;
using Micser.App.Resources;
using Micser.Common.Settings;
using Prism.Commands;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Micser.App.ViewModels
{
    public class SettingsViewModel : ViewModelNavigationAware
    {
        private readonly IDialogService _dialogService;
        private readonly ISettingsRegistry _settingsRegistry;
        private readonly SettingsSerializer _settingsSerializer;
        private readonly ISettingsService _settingsService;
        private IEnumerable<ISettingViewModel> _settings;

        public SettingsViewModel(
            ISettingsRegistry settingsRegistry,
            ISettingsService settingsService,
            IDialogService dialogService,
            SettingsSerializer settingsSerializer)
        {
            _settingsRegistry = settingsRegistry;
            _settingsService = settingsService;
            _dialogService = dialogService;
            _settingsSerializer = settingsSerializer;

            RefreshCommand = new DelegateCommand(async () => await LoadAsync(), () => !IsBusy);
            ImportCommand = new DelegateCommand(async () => await ImportAsync(), () => !IsBusy);
            ExportCommand = new DelegateCommand(async () => await ExportAsync(), () => !IsBusy);

            AddCommandBinding(CustomApplicationCommands.Refresh, RefreshCommand);
            AddCommandBinding(CustomApplicationCommands.Import, ImportCommand);
            AddCommandBinding(CustomApplicationCommands.Export, ExportCommand);
        }

        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand RefreshCommand { get; }

        public IEnumerable<ISettingViewModel> Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        protected override async void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            await LoadAsync();
        }

        private Task ExportAsync()
        {
            if (_dialogService.ShowSaveFileDialog(
                new FileDialogOptions(Strings.ExportSettingsDialogTitle, ".json"),
                out var fileName))
            {
                var result = _settingsSerializer.Export(fileName);

                // todo show notification
                if (result)
                {
                }
            }

            return Task.CompletedTask;
        }

        private async Task ImportAsync()
        {
            if (_dialogService.ShowSaveFileDialog(
                new FileDialogOptions(Strings.ExportSettingsDialogTitle, ".json"),
                out var fileName))
            {
                var result = _settingsSerializer.Import(fileName);
                if (result)
                {
                    await LoadAsync();
                }
            }
        }

        private async Task LoadAsync()
        {
            IsBusy = true;
            await _settingsService.LoadAsync();

            await Task.Run(() =>
            {
                var settings = _settingsRegistry
                    .Items
                    .Where(s => !s.IsHidden)
                    .Select<SettingDefinition, ISettingViewModel>(s =>
                    {
                        switch (s.Type)
                        {
                            case SettingType.Boolean:
                                return new BooleanSettingViewModel(s, _settingsService);

                            case SettingType.Integer:
                                return new IntegerSettingViewModel(s, _settingsService);

                            case SettingType.Decimal:
                                return new DecimalSettingViewModel(s, _settingsService);

                            case SettingType.List:
                                return new ListSettingViewModel(s, _settingsService);

                            default:
                                return new StringSettingViewModel(s, _settingsService);
                        }
                    });

                Settings = settings.ToArray();
            });
            IsBusy = false;
        }
    }
}