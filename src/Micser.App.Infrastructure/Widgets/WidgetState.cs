using Micser.Common.Modules;
using System.Windows;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetState : IModuleViewState
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
}