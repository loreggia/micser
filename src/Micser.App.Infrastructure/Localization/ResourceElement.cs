using System;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace Micser.App.Infrastructure.Localization
{
    public class ResourceElement : FrameworkElement, INotifyPropertyChanged, IDisposable
    {
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(ResourceElement), new PropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty ResourceManagerProperty = DependencyProperty.Register(
            nameof(ResourceManager), typeof(ResourceManager), typeof(ResourceElement), new PropertyMetadata(null, OnPropertyChanged));

        private object _content;

        static ResourceElement()
        {
        }

        public ResourceElement(ResourceManager resourceManager, string key)
            : this()
        {
            ResourceManager = resourceManager;
            Key = key;

            Loaded += OnLoaded;
        }

        public ResourceElement()
        {
            LocalizationManager.Instance.UiCultureChanged += OnUiCultureChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object Content
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

        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public ResourceManager ResourceManager
        {
            get => (ResourceManager)GetValue(ResourceManagerProperty);
            set => SetValue(ResourceManagerProperty, value);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.Instance.UiCultureChanged -= OnUiCultureChanged;
            }
        }

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
            Content = (key != null ? resourceManager?.GetString(Key, LocalizationManager.Instance.UiCulture) : null) ?? $"#{Key}#";
        }
    }
}