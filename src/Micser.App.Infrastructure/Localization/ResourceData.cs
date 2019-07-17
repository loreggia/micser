using System;
using System.Resources;

namespace Micser.App.Infrastructure.Localization
{
    public class ResourceData : Bindable, IDisposable
    {
        private readonly string _key;
        private readonly ResourceManager _resourceManager;

        private string _value;

        public ResourceData(ResourceManager resourceManager, string key)
        {
            _resourceManager = resourceManager;
            _key = key;

            LocalizationManager.Instance.UiCultureChanged += OnUiCultureChanged;

            UpdateValue();
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return Value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.Instance.UiCultureChanged -= OnUiCultureChanged;
            }
        }

        private void OnUiCultureChanged(object sender, EventArgs e)
        {
            UpdateValue();
        }

        private void UpdateValue()
        {
            Value = _resourceManager?.GetString(_key, LocalizationManager.Instance.UiCulture) ?? $"#{_key}#";
        }
    }
}