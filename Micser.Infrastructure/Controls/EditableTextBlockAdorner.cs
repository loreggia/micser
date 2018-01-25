using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Infrastructure.Controls
{
    public class EditableTextBlockAdorner : Adorner
    {
        private readonly VisualCollection _collection;

        private readonly EditableTextBlock _textBlock;
        private readonly TextBox _textBox;

        public EditableTextBlockAdorner(EditableTextBlock editableTextBlock)
            : base(editableTextBlock)
        {
            _collection = new VisualCollection(this);
            _textBox = new TextBox();
            _textBlock = editableTextBlock;
            var binding = new Binding(nameof(EditableTextBlock.Text)) { Source = editableTextBlock };
            _textBox.SetBinding(TextBox.TextProperty, binding);
            _textBox.MaxLength = editableTextBlock.MaxLength;
            _textBox.KeyUp += TextBox_KeyUp;
            _textBox.LostFocus += TextBox_LostFocus;
            _collection.Add(_textBox);
        }

        public event KeyEventHandler TextBoxKeyUp
        {
            add => _textBox.KeyUp += value;
            remove => _textBox.KeyUp -= value;
        }

        public event RoutedEventHandler TextBoxLostFocus
        {
            add => _textBox.LostFocus += value;
            remove => _textBox.LostFocus -= value;
        }

        protected override int VisualChildrenCount => _collection.Count;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _textBox.Arrange(new Rect(0, 0, _textBlock.DesiredSize.Width, _textBlock.DesiredSize.Height * 1.5));
            _textBox.Focus();
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _collection[index];
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(null, new Pen
            {
                Brush = Brushes.Gold,
                Thickness = 2
            }, new Rect(0, 0, _textBlock.DesiredSize.Width, _textBlock.DesiredSize.Height * 1.5));
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var expression = _textBox.GetBindingExpression(TextBox.TextProperty);
                expression?.UpdateSource();
                _textBlock.IsEditing = false;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _textBlock.IsEditing = false;
        }
    }
}