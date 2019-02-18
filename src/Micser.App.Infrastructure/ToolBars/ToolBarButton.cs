using System.Windows.Input;

namespace Micser.App.Infrastructure.ToolBars
{
    public class ToolBarButton : ToolBarItem
    {
        public ICommand Command { get; set; }
    }
}