using System;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace Micser.App.Infrastructure.Localization
{
    public class LocalizationManager
    {
        public static readonly DependencyProperty ResourceManagerProperty = DependencyProperty.RegisterAttached(
            "ResourceManager", typeof(ResourceManager), typeof(LocalizationManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        private static LocalizationManager _instance;

        private CultureInfo _uiCulture;

        public event EventHandler UiCultureChanged;

        public static LocalizationManager Instance => _instance ?? (_instance = new LocalizationManager());

        public CultureInfo UiCulture
        {
            get => _uiCulture;
            set
            {
                if (!Equals(_uiCulture, value))
                {
                    _uiCulture = value;
                    OnUiCultureChanged(EventArgs.Empty);
                }
            }
        }

        public static ResourceManager GetResourceManager(DependencyObject element)
        {
            return (ResourceManager)element.GetValue(ResourceManagerProperty);
        }

        public static void SetResourceManager(DependencyObject element, ResourceManager value)
        {
            element.SetValue(ResourceManagerProperty, value);
        }

        private void OnUiCultureChanged(EventArgs e)
        {
            UiCultureChanged?.Invoke(this, e);
        }
    }
}