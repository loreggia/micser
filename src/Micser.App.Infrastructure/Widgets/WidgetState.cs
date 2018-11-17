using Micser.Infrastructure.Models;
using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetState : ModuleViewState
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
}