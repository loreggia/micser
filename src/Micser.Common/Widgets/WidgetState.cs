using System.Collections.Generic;
using System.Windows;

namespace Micser.Common.Widgets
{
    public class WidgetState
    {
        public WidgetState()
        {
            Data = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Data { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
}