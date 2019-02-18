using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using System.Collections.Generic;
using System.Linq;

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
        }

        public IEnumerable<SettingViewModel> Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        protected override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            var settings = _settingsRegistry.Items.Select(s =>
            {
                switch (s.Type)
                {
                    case SettingType.Boolean:
                        return (SettingViewModel)new BooleanSettingViewModel(s, _settingsService.GetSetting<bool>(s.Key));

                    case SettingType.Integer:
                        return new IntegerSettingViewModel(s, _settingsService.GetSetting<long>(s.Key));

                    case SettingType.Decimal:
                        return new DecimalSettingViewModel(s, _settingsService.GetSetting<double>(s.Key));

                    default:
                        return new StringSettingViewModel(s, _settingsService.GetSetting<string>(s.Key));
                }
            });

            Settings = settings;
        }
    }

    public abstract class SettingViewModel : ViewModel
    {
        protected object DefaultValue;

        private string _description;
        private string _key;
        private string _name;

        protected SettingViewModel(SettingDefinition setting)
        {
            Key = setting.Key;
            Name = setting.Name;
            Description = setting.Description;
            DefaultValue = setting.DefaultValue;
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }

    internal class BooleanSettingViewModel : SettingViewModel
    {
        private bool _value;

        public BooleanSettingViewModel(SettingDefinition setting, bool value)
            : base(setting)
        {
            Value = value;
        }

        public bool Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    internal class DecimalSettingViewModel : SettingViewModel
    {
        private double _value;

        public DecimalSettingViewModel(SettingDefinition setting, double value)
            : base(setting)
        {
            Value = value;
        }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    internal class IntegerSettingViewModel : SettingViewModel
    {
        private long _value;

        public IntegerSettingViewModel(SettingDefinition setting, long value)
            : base(setting)
        {
            Value = value;
        }

        public long Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    internal class StringSettingViewModel : SettingViewModel
    {
        private string _value;

        public StringSettingViewModel(SettingDefinition setting, string value)
            : base(setting)
        {
            Value = value;
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}