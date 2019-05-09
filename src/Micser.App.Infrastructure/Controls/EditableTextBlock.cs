using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Controls
{
    /// <summary>
    /// Control that looks like a <see cref="TextBlock"/> but allows switching to a <see cref="TextBox"/> to change the contents.
    /// </summary>
    [TemplatePart(Name = PartNameTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartNameToggleButton, Type = typeof(ButtonBase))]
    public class EditableTextBlock : Control
    {
        public const string PartNameTextBox = "PART_TextBox";
        public const string PartNameToggleButton = "PART_ToggleButton";

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));

        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            nameof(MaxLength), typeof(int), typeof(EditableTextBlock), new PropertyMetadata(0));

        public static readonly DependencyProperty ShowButtonProperty = DependencyProperty.Register(
            nameof(ShowButton), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(true));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(EditableTextBlock), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private TextBox _textBox;

        private ButtonBase _toggleButton;

        static EditableTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock), new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public bool ShowButton
        {
            get => (bool)GetValue(ShowButtonProperty);
            set => SetValue(ShowButtonProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool FocusTextBox()
        {
            if (_textBox != null && IsEditing)
            {
                return _textBox.Focus();
            }

            return false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox = GetTemplateChild(PartNameTextBox) as TextBox;
            _toggleButton = GetTemplateChild(PartNameToggleButton) as ButtonBase;

            if (_textBox != null)
            {
                _textBox.KeyDown += OnTextBoxKeyDown;
            }

            if (_toggleButton != null)
            {
                _toggleButton.Click += OnToggleButtonClick;
            }
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IsEditing = false;
            }
        }

        private void OnToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsEditing)
            {
                FocusTextBox();
            }
        }
    }
}