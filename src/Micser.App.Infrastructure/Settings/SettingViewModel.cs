using Micser.App.Infrastructure.Extensions;
using Prism.Commands;
using System.Collections.Generic;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// Common ancestor for the different setting view models.
    /// </summary>
    public interface ISettingViewModel
    {
    }

    /// <summary>
    /// A setting for boolean values / checkboxes.
    /// </summary>
    public class BooleanSettingViewModel : SettingViewModel<bool>
    {
        /// <inheritdoc />
        public BooleanSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    /// <summary>
    /// A setting for decimal number values.
    /// </summary>
    public class DecimalSettingViewModel : SettingViewModel<double>
    {
        /// <inheritdoc />
        public DecimalSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    /// <summary>
    /// A setting for whole number values.
    /// </summary>
    public class IntegerSettingViewModel : SettingViewModel<long>
    {
        /// <inheritdoc />
        public IntegerSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }

    /// <summary>
    /// A list setting.
    /// </summary>
    public class ListSettingViewModel : SettingViewModel<object>
    {
        private IDictionary<object, string> _list;

        /// <inheritdoc />
        public ListSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
            _list = setting.List;
        }

        /// <summary>
        /// Gets the list of available setting values.
        /// </summary>
        public IDictionary<object, string> List
        {
            get => _list;
            protected set => SetProperty(ref _list, value);
        }
    }

    /// <summary>
    /// Base class for setting view models.
    /// </summary>
    /// <typeparam name="T">The type of the value stored by the setting.</typeparam>
    public abstract class SettingViewModel<T> : ViewModel, ISettingViewModel
    {
        private readonly ISettingsService _settingsService;
        private bool _isChanged;
        private bool _isEnabled;
        private T _value;

        /// <inheritdoc />
        protected SettingViewModel(SettingDefinition definition, ISettingsService settingsService)
        {
            Definition = definition;
            _settingsService = settingsService;

            ApplyCommand = new DelegateCommand(Apply).ObservesCanExecute(() => IsChanged);

            settingsService.SettingChanged += OnSettingChanged;
            _value = settingsService.GetSetting<T>(definition.Key);

            UpdateIsEnabled();
        }

        /// <summary>
        /// Explicitly saves the setting.
        /// </summary>
        public ICommand ApplyCommand { get; set; }

        /// <summary>
        /// Gets the setting definition as registered during module initialization.
        /// </summary>
        public SettingDefinition Definition { get; }

        /// <summary>
        /// Gets a value that indicates whether the setting has been changed and not saved yet.
        /// </summary>
        public bool IsChanged
        {
            get => _isChanged;
            protected set => SetProperty(ref _isChanged, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the setting is enabled (can be changed). This is controlled by the <see cref="SettingDefinition.IsEnabled"/> function.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            protected set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// Gets or sets the setting value. If <see cref="SettingDefinition.IsAppliedInstantly"/> is true, the setting is saved immediately.
        /// </summary>
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

        /// <summary>
        /// Saves the setting.
        /// </summary>
        protected void Apply()
        {
            if (_settingsService.SetSetting(Definition.Key, Value))
            {
                IsChanged = false;
            }
        }

        /// <summary>
        /// Calls the <see cref="SettingDefinition.IsEnabled"/> function and assigns the value to <see cref="IsEnabled"/>.
        /// </summary>
        protected void UpdateIsEnabled()
        {
            IsEnabled = Definition.IsEnabled?.Invoke(_settingsService) != false;
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            UpdateIsEnabled();
        }
    }

    /// <summary>
    /// A string value setting.
    /// </summary>
    public class StringSettingViewModel : SettingViewModel<string>
    {
        /// <inheritdoc />
        public StringSettingViewModel(SettingDefinition setting, ISettingsService settingsService)
            : base(setting, settingsService)
        {
        }
    }
}