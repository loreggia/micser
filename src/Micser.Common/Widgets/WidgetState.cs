using System.Windows;

namespace Micser.Common.Widgets
{
    public sealed class WidgetState : StateDictionary
    {
        public Point Position { get; set; }

        public Size Size { get; set; }
    }
}