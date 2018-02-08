using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
{
    public enum ConnectorOrientation
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }

    // provides compact info about a connector; used for the
    // routing algorithm, instead of hand over a full fledged Connector
    internal struct ConnectorInfo
    {
        public ConnectorOrientation Orientation { get; set; }
        public Point Position { get; set; }
        public double WidgetLeft { get; set; }
        public Size WidgetSize { get; set; }
        public double WidgetTop { get; set; }
    }

    public class Connector : Control, INotifyPropertyChanged
    {
        // the Widget this Connector belongs to;
        // retrieved from DataContext, which is set in the
        // Widget template
        private Widget _parentWidget;

        // keep track of connections that link to this connector
        private List<Connection> connections;

        // drag start point, relative to the WidgetPanel
        private Point? dragStartPoint = null;

        // center position of this Connector relative to the WidgetPanel
        private Point position;

        public Connector()
        {
            // fired when layout changes
            base.LayoutUpdated += new EventHandler(Connector_LayoutUpdated);
        }

        public List<Connection> Connections
        {
            get
            {
                if (connections == null)
                    connections = new List<Connection>();
                return connections;
            }
        }

        public ConnectorOrientation Orientation { get; set; }

        public Widget ParentWidget
        {
            get
            {
                if (_parentWidget == null)
                    _parentWidget = this.DataContext as Widget;

                return _parentWidget;
            }
        }

        public Point Position
        {
            get { return position; }
            set
            {
                if (position != value)
                {
                    position = value;
                    OnPropertyChanged("Position");
                }
            }
        }

        internal ConnectorInfo GetInfo()
        {
            ConnectorInfo info = new ConnectorInfo();
            info.WidgetLeft = WidgetPanel.GetLeft(this.ParentWidget);
            info.WidgetTop = WidgetPanel.GetTop(this.ParentWidget);
            info.WidgetSize = new Size(this.ParentWidget.ActualWidth, this.ParentWidget.ActualHeight);
            info.Orientation = this.Orientation;
            info.Position = this.Position;
            return info;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            WidgetPanel canvas = GetWidgetPanel(this);
            if (canvas != null)
            {
                // position relative to WidgetPanel
                this.dragStartPoint = new Point?(e.GetPosition(canvas));
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // if mouse button is not pressed we have no drag operation, ...
            if (e.LeftButton != MouseButtonState.Pressed)
                this.dragStartPoint = null;

            // but if mouse button is pressed and start point value is set we do have one
            if (this.dragStartPoint.HasValue)
            {
                // create connection adorner
                WidgetPanel canvas = GetWidgetPanel(this);
                if (canvas != null)
                {
                    AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                    if (adornerLayer != null)
                    {
                        ConnectorAdorner adorner = new ConnectorAdorner(canvas, this);
                        if (adorner != null)
                        {
                            adornerLayer.Add(adorner);
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        // when the layout changes we update the position property
        private void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            WidgetPanel panel = GetWidgetPanel(this);
            if (panel != null)
            {
                //get centre position of this Connector relative to the WidgetPanel
                this.Position = this.TransformToAncestor(panel).Transform(new Point(this.Width / 2, this.Height / 2));
            }
        }

        // iterate through visual tree to get parent WidgetPanel
        private WidgetPanel GetWidgetPanel(DependencyObject element)
        {
            while (element != null && !(element is WidgetPanel))
                element = VisualTreeHelper.GetParent(element);

            return element as WidgetPanel;
        }

        #region INotifyPropertyChanged Members

        // we could use DependencyProperties as well to inform others of property changes
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion INotifyPropertyChanged Members
    }
}