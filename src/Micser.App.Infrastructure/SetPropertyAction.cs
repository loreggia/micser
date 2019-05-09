using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Interactivity;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Sets the designated property to the supplied value. <see cref="TargetObject"/> optionally designates the object on which to set the property.
    /// If <see cref="TargetObject"/> is not supplied then the property is set on the object to which the trigger is attached.
    /// </summary>
    public class SetPropertyAction : TriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(nameof(PropertyName), typeof(string), typeof(SetPropertyAction));
        public static readonly DependencyProperty PropertyValueProperty = DependencyProperty.Register(nameof(PropertyValue), typeof(object), typeof(SetPropertyAction));
        public static readonly DependencyProperty TargetObjectProperty = DependencyProperty.Register(nameof(TargetObject), typeof(object), typeof(SetPropertyAction));

        /// <summary>
        ///     The property to be executed in response to the trigger.
        /// </summary>
        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        /// <summary>
        /// The value to set the property to.
        /// </summary>
        public object PropertyValue
        {
            get => GetValue(PropertyValueProperty);
            set => SetValue(PropertyValueProperty, value);
        }

        /// <summary>
        /// Specifies the object upon which to set the property.
        /// </summary>
        public object TargetObject
        {
            get => GetValue(TargetObjectProperty);
            set => SetValue(TargetObjectProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            var value = PropertyValue;
            var target = TargetObject ?? AssociatedObject;
            var propertyInfo = target.GetType().GetProperty(PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
            if (propertyInfo != null && value != null)
            {
                var valueType = value.GetType();
                var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                if (converter.CanConvertFrom(valueType))
                {
                    value = converter.ConvertFrom(value);
                }
            }

            propertyInfo?.SetValue(target, value);
        }
    }
}