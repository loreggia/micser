using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Micser.Core.Controls
{
    public class EditableTextBlock : Control
    {
        public const string PartTextBlockName = "PART_TextBlock";
        public const string PartTextBoxName = "PART_TextBox";

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(EditableTextBlock), new PropertyMetadata(null));

        private string _previousText;
        private TextBlock _textBlock;
        private TextBox _textBox;

        static EditableTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock), new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_textBlock != null)
            {
                _textBlock.MouseLeftButtonDown -= OnTextBlockMouseLeftButtonDown;
            }
            if (_textBox != null)
            {
                _textBox.KeyDown -= OnTextBoxKeyDown;
            }

            _textBlock = GetTemplateChild(PartTextBlockName) as TextBlock;
            _textBox = GetTemplateChild(PartTextBoxName) as TextBox;

            if (_textBlock == null || _textBox == null)
            {
                throw new InvalidOperationException("Invalid template.");
            }

            _textBlock.MouseLeftButtonDown += OnTextBlockMouseLeftButtonDown;
            _textBox.KeyDown += OnTextBoxKeyDown;
        }

        private void OnTextBlockMouseLeftButtonDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ClickCount == 2)
            {
                IsEditing = true;
                _textBox.SelectAll();
                _previousText = Text;

                Dispatcher.BeginInvoke(new Func<bool>(() => _textBox.Focus()));
            }
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsEditing)
            {
                return;
            }

            if (e.Key == Key.Enter)
            {
                IsEditing = false;
            }
            else if (e.Key == Key.Escape)
            {
                IsEditing = false;
                Text = _previousText;
            }
        }
    }
}