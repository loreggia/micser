using System.Windows;
using Micser.Common.Modules;

namespace Micser.App.Infrastructure.Widgets
{
    public class WidgetState : IModuleViewState
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
    }
}