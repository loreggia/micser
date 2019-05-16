using System.Windows;
using System.Windows.Controls;

namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// A container control for a setting hosted in a <see cref="SettingsPanel"/>.
    /// </summary>
    public class SettingContainer : ContentControl
    {
        /// <summary>
        /// The description that is shown as a tooltip and in the info popup.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(SettingContainer), new PropertyMetadata(default(string)));

        /// <summary>
        /// The main label text (setting name).
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(SettingContainer), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the description.
        /// Wraps the <see cref="DescriptionProperty"/> dependency property.
        /// </summary>
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the label text.
        /// Wraps the <see cref="LabelProperty"/> dependency property.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="SettingsPanel"/> parent control that this container is a child of.
        /// </summary>
        public SettingsPanel ParentPanel => ItemsControl.ItemsControlFromItemContainer(this) as SettingsPanel;
    }
}