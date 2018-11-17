using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Controls
{
    public class EditableTextBlock : TextBlock
    {
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
            nameof(IsEditing), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false, IsEditingPropertyChanged));

        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            nameof(MaxLength), typeof(int), typeof(EditableTextBlock), new PropertyMetadata(0));

        private EditableTextBlockAdorner _adorner;

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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                IsEditing = true;
            }
        }

        private static void IsEditingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (EditableTextBlock)d;

            //Get the adorner layer of the uielement (here TextBlock)
            var layer = AdornerLayer.GetAdornerLayer(textBlock);

            //If the IsInEditMode set to true means the user has enabled the edit mode then
            //add the adorner to the adorner layer of the TextBlock.
            if (textBlock.IsEditing)
            {
                if (textBlock._adorner == null)
                {
                    textBlock._adorner = new EditableTextBlockAdorner(textBlock);
                }
                layer.Add(textBlock._adorner);
            }
            else
            {
                //Remove the adorner from the adorner layer.
                var adorners = layer.GetAdorners(textBlock);
                if (adorners != null)
                {
                    foreach (var adorner in adorners)
                    {
                        if (adorner is EditableTextBlockAdorner)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }

                //Update the textblock's text binding.
                var expression = textBlock.GetBindingExpression(TextProperty);
                expression?.UpdateTarget();
            }
        }
    }
}