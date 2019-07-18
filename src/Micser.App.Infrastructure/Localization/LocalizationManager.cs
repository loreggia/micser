using System;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace Micser.App.Infrastructure.Localization
{
    /// <summary>
    /// Provides global UI culture settings.
    /// </summary>
    public static class LocalizationManager
    {
        /// <summary>
        /// Dependency property for the ResourceManager attached property.
        /// </summary>
        public static readonly DependencyProperty ResourceManagerProperty = DependencyProperty.RegisterAttached(
            "ResourceManager", typeof(ResourceManager), typeof(LocalizationManager), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        private static CultureInfo _uiCulture;

        /// <summary>
        /// Fires when the <see cref="UiCulture"/> has changed.
        /// </summary>
        public static event EventHandler UiCultureChanged;

        /// <summary>
        /// Gets or sets the current UI culture.
        /// </summary>
        public static CultureInfo UiCulture
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

        /// <summary>
        /// Gets the ResourceManager attached property value. This property is inheritable.
        /// </summary>
        public static ResourceManager GetResourceManager(DependencyObject element)
        {
            return (ResourceManager)element.GetValue(ResourceManagerProperty);
        }

        /// <summary>
        /// Sets the ResourceManager attached property value. This property is inheritable.
        /// </summary>
        public static void SetResourceManager(DependencyObject element, ResourceManager value)
        {
            element.SetValue(ResourceManagerProperty, value);
        }

        private static void OnUiCultureChanged(EventArgs e)
        {
            UiCultureChanged?.Invoke(null, e);
        }
    }
}