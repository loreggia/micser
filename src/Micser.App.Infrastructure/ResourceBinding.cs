using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Markup extension that allows binding the key of a dynamic resource.
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/a/28647821
    /// </remarks>
    public class ResourceBinding : MarkupExtension
    {
#pragma warning disable 1591

        public static readonly DependencyProperty ResourceBindingKeyHelperProperty =
            DependencyProperty.RegisterAttached("ResourceBindingKeyHelper", typeof(object), typeof(ResourceBinding), new PropertyMetadata(null, ResourceKeyChanged));

        private readonly Binding _binding;

        /// <inheritdoc />
        public ResourceBinding()
        {
            _binding = new Binding();
        }

        /// <summary>
        /// Creates an instance of the <see cref="ResourceBinding"/> class using the specified binding path.
        /// </summary>
        public ResourceBinding(string path)
            : this()
        {
            Path = new PropertyPath(path);
        }

        /// <summary>
        /// The Converter to apply.
        /// </summary>
        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get => _binding.Converter;
            set => _binding.Converter = value;
        }

        /// <summary>
        /// Culture in which to evaluate the converter.
        /// </summary>
        [DefaultValue(null)]
        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter))]
        public CultureInfo ConverterCulture
        {
            get => _binding.ConverterCulture;
            set => _binding.ConverterCulture = value;
        }

        /// <summary>
        /// The parameter to pass to converter.
        /// </summary>
        [DefaultValue(null)]
        public object ConverterParameter
        {
            get => _binding.ConverterParameter;
            set => _binding.ConverterParameter = value;
        }

        /// <summary>
        /// Name of the element to use as the source.
        /// </summary>
        [DefaultValue(null)]
        public string ElementName
        {
            get => _binding.ElementName;
            set => _binding.ElementName = value;
        }

        /// <summary>
        /// Value to use when source cannot provide a value.
        /// </summary>
        /// <remarks>
        /// Initialized to DependencyProperty.UnsetValue; if FallbackValue is not set, BindingExpression
        /// will return target property's default when Binding cannot get a real value.
        /// </remarks>
        public object FallbackValue
        {
            get => _binding.FallbackValue;
            set => _binding.FallbackValue = value;
        }

        /// <summary>
        /// Binding mode.
        /// </summary>
        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode
        {
            get => _binding.Mode;
            set => _binding.Mode = value;
        }

        /// <summary>
        /// The source path (for CLR bindings).
        /// </summary>
        public PropertyPath Path
        {
            get => _binding.Path;
            set => _binding.Path = value;
        }

        /// <summary>
        /// Description of the object to use as the source, relative to the target element.
        /// </summary>
        [DefaultValue(null)]
        public RelativeSource RelativeSource
        {
            get => _binding.RelativeSource;
            set => _binding.RelativeSource = value;
        }

        /// <summary>
        /// The source path (for CLR bindings).
        /// </summary>
        public object Source
        {
            get => _binding.Source;
            set => _binding.Source = value;
        }

        /// <summary>
        /// Update type.
        /// </summary>
        [DefaultValue(UpdateSourceTrigger.Default)]
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get => _binding.UpdateSourceTrigger;
            set => _binding.UpdateSourceTrigger = value;
        }

        /// <summary>
        /// The XPath path (for XML bindings).
        /// </summary>
        [DefaultValue(null)]
        public string XPath
        {
            get => _binding.XPath;
            set => _binding.XPath = value;
        }

        // ReSharper disable once UnusedMember.Global
        public static object GetResourceBindingKeyHelper(DependencyObject obj)
        {
            return obj.GetValue(ResourceBindingKeyHelperProperty);
        }

        // ReSharper disable once UnusedMember.Global
        public static void SetResourceBindingKeyHelper(DependencyObject obj, object value)
        {
            obj.SetValue(ResourceBindingKeyHelperProperty, value);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTargetService = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            if (provideValueTargetService == null)
            {
                return null;
            }

            if (provideValueTargetService.TargetObject != null &&
                provideValueTargetService.TargetObject.GetType().FullName == "System.Windows.SharedDp")
            {
                return this;
            }

            var targetObject = provideValueTargetService.TargetObject as FrameworkElement;
            var targetSetter = provideValueTargetService.TargetObject as Setter;
            var targetProperty = provideValueTargetService.TargetProperty as DependencyProperty ?? targetSetter?.Property;
            if (targetObject == null && targetSetter == null || targetProperty == null)
            {
                return null;
            }

            var multiBinding = new MultiBinding
            {
                Converter = HelperConverter.Current,
                ConverterParameter = targetProperty
            };

            multiBinding.Bindings.Add(_binding);

            multiBinding.NotifyOnSourceUpdated = true;

            if (targetObject != null)
            {
                targetObject.SetBinding(ResourceBindingKeyHelperProperty, multiBinding);
            }
            else
            {
                targetSetter.Property = ResourceBindingKeyHelperProperty;
                return multiBinding;
            }

            return null;
        }

        private static void ResourceKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = d as FrameworkElement;
            var newVal = e.NewValue as Tuple<object, DependencyProperty>;

            if (target == null || newVal == null)
            {
                return;
            }

            var dp = newVal.Item2;

            if (newVal.Item1 == null)
            {
                target.SetValue(dp, dp.GetMetadata(target).DefaultValue);
                return;
            }

            target.SetResourceReference(dp, newVal.Item1);
        }

        private class HelperConverter : IMultiValueConverter
        {
            public static readonly HelperConverter Current = new HelperConverter();

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                return Tuple.Create(values[0], (DependencyProperty)parameter);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning restore 1591
    }
}