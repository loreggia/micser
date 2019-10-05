using System;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace Micser.App.Infrastructure.Localization
{
    /// <summary>
    /// A <see cref="FrameworkElement"/> that replaces itself with a bound resource value for the current UI culture.
    /// </summary>
    public class ResourceElement : FrameworkElement, INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// DependencyProperty for the <see cref="Key"/> property.
        /// </summary>
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(ResourceElement), new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// DependencyProperty for the <see cref="ResourceManager"/> property.
        /// </summary>
        public static readonly DependencyProperty ResourceManagerProperty = DependencyProperty.Register(
            nameof(ResourceManager), typeof(ResourceManager), typeof(ResourceElement), new PropertyMetadata(null, OnPropertyChanged));

        private string _content;

        /// <inheritdoc />
        public ResourceElement(ResourceManager resourceManager, string key)
            : this()
        {
            ResourceManager = resourceManager;
            Key = key;

            Loaded += OnLoaded;
        }

        /// <inheritdoc />
        public ResourceElement()
        {
            LocalizationManager.UiCultureChanged += OnUiCultureChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the localized content. This is not a dependency property and will automatically get bound to the parent's content.
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the resource key.
        /// </summary>
        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the resource manager containing the resource. If this is null, the inherited value of the LocalizationManager.ResourceManager attached property is used.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get => (ResourceManager)GetValue(ResourceManagerProperty);
            set => SetValue(ResourceManagerProperty, value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the localized content.
        /// </summary>
        public override string ToString()
        {
            return Content;
        }

        /// <summary>
        /// Releases resources and unregisters events.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.UiCultureChanged -= OnUiCultureChanged;
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ResourceElement)d).UpdateContent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var parent = Parent;

            if (parent == null)
            {
                return;
            }

            // create a binding from the parent to this element's content.
            var binding = new Binding(nameof(Content))
            {
                Source = this,
                Mode = BindingMode.OneWay
            };

            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(parent, new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) }))
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(pd);

                if (ReferenceEquals(this, parent.GetValue(dpd.DependencyProperty)))
                {
                    BindingOperations.SetBinding(parent, dpd.DependencyProperty, binding);
                    break;
                }
            }
        }

        private void OnUiCultureChanged(object sender, EventArgs e)
        {
            UpdateContent();
        }

        private void UpdateContent()
        {
            var resourceManager = ResourceManager ?? LocalizationManager.GetResourceManager(this);
            var key = Key;
            Content = (key != null ? resourceManager?.GetString(Key, LocalizationManager.UiCulture) : null) ?? $"#{Key}#";
        }
    }
}