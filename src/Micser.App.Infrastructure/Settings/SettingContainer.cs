using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Settings
{
    /// <summary>
    /// A container control for a setting hosted in a <see cref="SettingsPanel"/>.
    /// </summary>
    public class SettingContainer : ContentControl
    {
        /// <summary>
        /// The command to execute when the "Apply" button is pressed.
        /// </summary>
        public static readonly DependencyProperty ApplyCommandProperty = DependencyProperty.Register(
            nameof(ApplyCommand), typeof(ICommand), typeof(SettingContainer), new PropertyMetadata(null));

        /// <summary>
        /// The description that is shown as a tooltip and in the info popup.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(SettingContainer), new PropertyMetadata(null));

        /// <summary>
        /// Indicates whether the control is busy (i.e. while saving).
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(SettingContainer), new PropertyMetadata(false));

        /// <summary>
        /// The main label text (setting name).
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(object), typeof(SettingContainer), new PropertyMetadata(null));

        /// <summary>
        /// A value indicating whether the "Apply" button is visible.
        /// </summary>
        public static readonly DependencyProperty ShowApplyButtonProperty = DependencyProperty.Register(
            nameof(ShowApplyButton), typeof(bool), typeof(SettingContainer), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command to execute when the "Apply" button is pressed.
        /// Wraps the <see cref="ApplyCommandProperty"/> dependency property.
        /// </summary>
        public ICommand ApplyCommand
        {
            get => (ICommand)GetValue(ApplyCommandProperty);
            set => SetValue(ApplyCommandProperty, value);
        }

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
        /// Gets or sets a value indicating whether the control is busy (i.e. while saving).
        /// Wraps the <see cref="IsBusyProperty"/> dependency property.
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        /// <summary>
        /// Gets or sets the label text.
        /// Wraps the <see cref="LabelProperty"/> dependency property.
        /// </summary>
        public object Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to show the "Apply" button.
        /// Wraps the <see cref="ShowApplyButtonProperty"/> dependency property.
        /// </summary>
        public bool ShowApplyButton
        {
            get => (bool)GetValue(ShowApplyButtonProperty);
            set => SetValue(ShowApplyButtonProperty, value);
        }
    }
}