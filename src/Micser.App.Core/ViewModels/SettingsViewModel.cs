using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Commands;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Settings;
using Micser.Common.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Micser.App.ViewModels
{
    public class SettingsViewModel : ViewModelNavigationAware
    {
        private readonly ISettingsRegistry _settingsRegistry;
        private readonly SettingsSerializer _settingsSerializer;
        private readonly ISettingsService _settingsService;
        private IEnumerable<ISettingViewModel> _settings;

        public SettingsViewModel(ISettingsRegistry settingsRegistry, ISettingsService settingsService, SettingsSerializer settingsSerializer)
        {
            _settingsRegistry = settingsRegistry;
            _settingsService = settingsService;
            _settingsSerializer = settingsSerializer;

            RefreshCommand = new DelegateCommand(async _ => await LoadAsync(), _ => !IsBusy);
            ImportCommand = new DelegateCommand(async _ => await ImportAsync(), _ => !IsBusy);
            ExportCommand = new DelegateCommand(async _ => await ExportAsync(), _ => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Refresh, RefreshCommand);
            AddCommandBinding(CustomApplicationCommands.Import, ImportCommand);
            AddCommandBinding(CustomApplicationCommands.Export, ExportCommand);

            ImportFileRequest = new InteractionRequest<IConfirmation>();
            ExportFileRequest = new InteractionRequest<IConfirmation>();
        }

        public ICommand ExportCommand { get; }

        public InteractionRequest<IConfirmation> ExportFileRequest { get; }

        public ICommand ImportCommand { get; }

        public InteractionRequest<IConfirmation> ImportFileRequest { get; }

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
            var confirmation = new FileDialogConfirmation { Title = Resources.ExportSettingsDialogTitle, DefaultExtension = ".json" };
            confirmation.AddFilter(Resources.JsonFiles, "*.json");
            ExportFileRequest.Raise(confirmation, c =>
            {
                if (!c.Confirmed)
                {
                    return;
                }

                var fileName = c.Content as string;
                var result = _settingsSerializer.Export(fileName);

                // todo show notification
                if (result)
                {
                }
            });
            return Task.CompletedTask;
        }

        private Task ImportAsync()
        {
            var confirmation = new FileDialogConfirmation { Title = Resources.ImportSettingsDialogTitle, DefaultExtension = ".json" };
            confirmation.AddFilter(Resources.JsonFiles, "*.json");
            ImportFileRequest.Raise(confirmation, async c =>
            {
                if (!c.Confirmed)
                {
                    return;
                }

                var fileName = c.Content as string;
                var result = _settingsSerializer.Import(fileName);
                if (result)
                {
                    await LoadAsync();
                }
            });
            return Task.CompletedTask;
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