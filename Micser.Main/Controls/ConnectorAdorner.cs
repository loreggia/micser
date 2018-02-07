using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Micser.Main.Controls
{
    public class ConnectorAdorner : Adorner
    {
        private readonly Pen _drawingPen;
        private readonly Connector _sourceConnector;
        private readonly WidgetPanel _widgetPanel;

        public ConnectorAdorner(WidgetPanel widgetPanel, Connector sourceConnector)
            : base(widgetPanel)
        {
            _widgetPanel = widgetPanel;
            _sourceConnector = sourceConnector;
            _drawingPen = new Pen(Brushes.LightSlateGray, 1);
            _drawingPen.LineJoin = PenLineJoin.Round;

            Cursor = Cursors.Cross;
        }
    }
}