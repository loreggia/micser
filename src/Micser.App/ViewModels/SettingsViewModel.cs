using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Interaction;
using Micser.App.Infrastructure.Settings;
using Micser.App.Properties;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
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
        private IEnumerable<SettingViewModel> _settings;

        public SettingsViewModel(ISettingsRegistry settingsRegistry, ISettingsService settingsService, SettingsSerializer settingsSerializer)
        {
            _settingsRegistry = settingsRegistry;
            _settingsService = settingsService;
            _settingsSerializer = settingsSerializer;

            RefreshCommand = new DelegateCommand(async () => await LoadAsync(), () => !IsBusy);
            ImportCommand = new DelegateCommand(async () => await ImportAsync(), () => !IsBusy);
            ExportCommand = new DelegateCommand(async () => await ExportAsync(), () => !IsBusy);
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

        public IEnumerable<SettingViewModel> Settings
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
            await Task.Run(() =>
            {
                var settings = _settingsRegistry
                    .Items
                    .Where(s => !s.IsHidden)
                    .Select<SettingDefinition, SettingViewModel>(s =>
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

    public abstract class SettingViewModel : ViewModel
    {
        private bool _isEnabled;

        protected SettingViewModel(SettingDefinition definition)
        {
            Definition = definition;
        }

        public SettingDefinition Definition { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }

    public abstract class SettingViewModel<T> : SettingViewModel
    {
        private readonly ISettingsService _settingsService;
        private T _value;

        protected SettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting)
        {
            _settingsService = settingsService;
            settingsService.SettingChanged += OnSettingChanged;
            _value = settingsService.GetSetting<T>(setting.Key);
        }

        public T Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    _settingsService.SetSetting(Definition.Key, Value);
                }
            }
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            IsEnabled = Definition.IsEnabled?.Invoke(_settingsService) != false;
        }
    }

    internal class BooleanSettingViewModel : SettingViewModel<bool>
    {
        public BooleanSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    internal class DecimalSettingViewModel : SettingViewModel<double>
    {
        public DecimalSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    internal class IntegerSettingViewModel : SettingViewModel<long>
    {
        public IntegerSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    internal class ListSettingViewModel : SettingViewModel<object>
    {
        private IDictionary<object, string> _list;

        public ListSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
            _list = setting.List;
        }

        public IDictionary<object, string> List
        {
            get => _list;
            set => SetProperty(ref _list, value);
        }
    }

    internal class StringSettingViewModel : SettingViewModel<string>
    {
        public StringSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }
}