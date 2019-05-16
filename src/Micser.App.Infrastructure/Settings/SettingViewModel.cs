using Micser.App.Infrastructure.Extensions;
using Prism.Commands;
using System.Collections.Generic;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Settings
{
    public class BooleanSettingViewModel : SettingViewModel<bool>
    {
        public BooleanSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    public class DecimalSettingViewModel : SettingViewModel<double>
    {
        public DecimalSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    public class IntegerSettingViewModel : SettingViewModel<long>
    {
        public IntegerSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    public class ListSettingViewModel : SettingViewModel<object>
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

    public abstract class SettingViewModel<T> : ViewModel, ISettingViewModel
    {
        private readonly ISettingsService _settingsService;
        private bool _isChanged;
        private bool _isEnabled;
        private T _value;

        protected SettingViewModel(SettingDefinition definition, ISettingsService settingsService)
        {
            Definition = definition;
            _settingsService = settingsService;

            ApplyCommand = new DelegateCommand(Apply).ObservesCanExecute(() => IsChanged);

            settingsService.SettingChanged += OnSettingChanged;
            _value = settingsService.GetSetting<T>(definition.Key);

            UpdateIsEnabled();
        }

        public ICommand ApplyCommand { get; set; }

        public SettingDefinition Definition { get; }

        public bool IsChanged
        {
            get => _isChanged;
            set => SetProperty(ref _isChanged, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public T Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    if (Definition.IsAppliedInstantly)
                    {
                        Apply();
                    }
                    IsChanged = true;
                }
            }
        }

        protected void UpdateIsEnabled()
        {
            IsEnabled = Definition.IsEnabled?.Invoke(_settingsService) != false;
        }

        private void Apply()
        {
            if (_settingsService.SetSetting(Definition.Key, Value))
            {
                IsChanged = false;
            }
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            UpdateIsEnabled();
        }
    }

    public class StringSettingViewModel : SettingViewModel<string>
    {
        public StringSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }
}