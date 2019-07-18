using System;
using System.Resources;

namespace Micser.App.Infrastructure.Localization
{
    /// <summary>
    /// Bindable resource value model that updates its value when the UI culture changes.
    /// </summary>
    public class ResourceData : Bindable, IDisposable
    {
        private readonly string _key;
        private readonly ResourceManager _resourceManager;

        private string _value;

        /// <inheritdoc />
        public ResourceData(ResourceManager resourceManager, string key)
        {
            _resourceManager = resourceManager;
            _key = key;

            LocalizationManager.UiCultureChanged += OnUiCultureChanged;

            UpdateValue();
        }

        /// <summary>
        /// Gets or sets the resource text.
        /// </summary>
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the <see cref="Value"/> property.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Disposes resources and unregisters events.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.UiCultureChanged -= OnUiCultureChanged;
            }
        }

        private void OnUiCultureChanged(object sender, EventArgs e)
        {
            UpdateValue();
        }

        private void UpdateValue()
        {
            Value = _resourceManager?.GetString(_key, LocalizationManager.UiCulture) ?? $"#{_key}#";
        }
    }
}