using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;

namespace DiagramDesigner
{
    public class WidgetPanel : Canvas
    {
        // start point of the rubberband drag operation
        private Point? _rubberbandSelectionStartPoint;

        // keep track of selected items

        public WidgetPanel()
        {
            AllowDrop = true;
            SelectedItems = new List<ISelectable>();
        }

        public IList<ISelectable> SelectedItems { get; }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = new Size();

            foreach (UIElement element in Children)
            {
                var left = GetLeft(element);
                var top = GetTop(element);
                left = double.IsNaN(left) ? 0 : left;
                top = double.IsNaN(top) ? 0 : top;

                //measure desired size for each child
                element.Measure(constraint);

                var desiredSize = element.DesiredSize;
                if (!double.IsNaN(desiredSize.Width) && !double.IsNaN(desiredSize.Height))
                {
                    size.Width = Math.Max(size.Width, left + desiredSize.Width);
                    size.Height = Math.Max(size.Height, top + desiredSize.Height);
                }
            }

            // add margin
            size.Width += 10;
            size.Height += 10;
            return size;
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetData(typeof(DragObject)) is DragObject dragObject && !string.IsNullOrEmpty(dragObject.Xaml))
            {
                var content = XamlReader.Load(XmlReader.Create(new StringReader(dragObject.Xaml)));

                if (content != null)
                {
                    var newItem = new Widget
                    {
                        Content = content
                    };

                    var position = e.GetPosition(this);

                    if (dragObject.DesiredSize.HasValue)
                    {
                        var desiredSize = dragObject.DesiredSize.Value;
                        newItem.Width = desiredSize.Width;
                        newItem.Height = desiredSize.Height;

                        SetLeft(newItem, Math.Max(0, position.X - newItem.Width / 2));
                        SetTop(newItem, Math.Max(0, position.Y - newItem.Height / 2));
                    }
                    else
                    {
                        SetLeft(newItem, Math.Max(0, position.X));
                        SetTop(newItem, Math.Max(0, position.Y));
                    }

                    Children.Add(newItem);

                    //update selection
                    foreach (var item in SelectedItems)
                    {
                        item.IsSelected = false;
                    }

                    SelectedItems.Clear();
                    newItem.IsSelected = true;
                    SelectedItems.Add(newItem);
                }

                e.Handled = true;
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (Equals(e.Source, this))
            {
                // in case that this click is the start for a
                // drag operation we cache the start point
                _rubberbandSelectionStartPoint = e.GetPosition(this);

                // if you click directly on the canvas all
                // selected items are 'de-selected'
                foreach (var item in SelectedItems)
                {
                    item.IsSelected = false;
                }

                SelectedItems.Clear();

                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _rubberbandSelectionStartPoint = null;
            }

            // ... but if mouse button is pressed and start
            // point value is set we do have one
            if (_rubberbandSelectionStartPoint.HasValue)
            {
                // create rubberband adorner
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    var adorner = new RubberbandAdorner(this, _rubberbandSelectionStartPoint);
                    adornerLayer.Add(adorner);
                }
            }
            e.Handled = true;
        }
    }
}