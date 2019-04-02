using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using Prism.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Micser.App.ViewModels
{
    public class SettingsViewModel : ViewModelNavigationAware
    {
        private readonly ISettingsRegistry _settingsRegistry;
        private readonly ISettingsService _settingsService;

        private IEnumerable<SettingViewModel> _settings;

        public SettingsViewModel(ISettingsRegistry settingsRegistry, ISettingsService settingsService)
        {
            _settingsRegistry = settingsRegistry;
            _settingsService = settingsService;

            SaveCommand = new DelegateCommand(Save, () => !IsBusy);
            AddCommandBinding(CustomApplicationCommands.Save, SaveCommand);
        }

        public ICommand SaveCommand { get; }

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

        private async Task LoadAsync()
        {
            IsBusy = true;
            await Task.Run(() =>
            {
                var settings = _settingsRegistry
                    .Items
                    .Where(s => !s.IsHidden)
                    .Select(s =>
                    {
                        switch (s.Type)
                        {
                            case SettingType.Boolean:
                                return (SettingViewModel)new BooleanSettingViewModel(s, _settingsService.GetSetting<bool>(s.Key));

                            case SettingType.Integer:
                                return new IntegerSettingViewModel(s, _settingsService.GetSetting<long>(s.Key));

                            case SettingType.Decimal:
                                return new DecimalSettingViewModel(s, _settingsService.GetSetting<double>(s.Key));

                            case SettingType.List:
                                return new ListSettingViewModel(s, _settingsService.GetSetting<object>(s.Key), s.List);

                            default:
                                return new StringSettingViewModel(s, _settingsService.GetSetting<object>(s.Key)?.ToString());
                        }
                    });

                Settings = settings.ToArray();
            });
            IsBusy = false;
        }

        private async void Save()
        {
            IsBusy = true;
            await Task.Run(() =>
            {
                foreach (var settingVm in Settings)
                {
                    _settingsService.SetSetting(settingVm.Definition.Key, settingVm.GetValue());
                }
            });
            IsBusy = false;
        }
    }

    public abstract class SettingViewModel : ViewModel
    {
        protected SettingViewModel(SettingDefinition definition)
        {
            Definition = definition;
        }

        public SettingDefinition Definition { get; }

        public abstract object GetValue();
    }

    public abstract class SettingViewModel<T> : SettingViewModel
    {
        private T _value;

        protected SettingViewModel(SettingDefinition setting, T value)
                    : base(setting)
        {
            Value = value;
        }

        public T Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public override object GetValue()
        {
            return Value;
        }
    }

    internal class BooleanSettingViewModel : SettingViewModel<bool>
    {
        public BooleanSettingViewModel(SettingDefinition setting, bool value)
            : base(setting, value)
        {
        }
    }

    internal class DecimalSettingViewModel : SettingViewModel<double>
    {
        public DecimalSettingViewModel(SettingDefinition setting, double value)
            : base(setting, value)
        {
        }
    }

    internal class IntegerSettingViewModel : SettingViewModel<long>
    {
        public IntegerSettingViewModel(SettingDefinition setting, long value)
            : base(setting, value)
        {
        }
    }

    internal class ListSettingViewModel : SettingViewModel<object>
    {
        private IDictionary<object, string> _list;

        public ListSettingViewModel(SettingDefinition setting, object value, IDictionary<object, string> list)
            : base(setting, value)
        {
            List = list;
        }

        public IDictionary<object, string> List
        {
            get => _list;
            set => SetProperty(ref _list, value);
        }
    }

    internal class StringSettingViewModel : SettingViewModel<string>
    {
        public StringSettingViewModel(SettingDefinition setting, string value)
            : base(setting, value)
        {
        }
    }
}