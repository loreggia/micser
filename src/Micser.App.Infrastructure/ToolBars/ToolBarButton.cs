using System;

namespace Micser.App.Infrastructure.ToolBars
{
    public class ToolBarButton : ToolBarItem
    {
        public Action<ToolBarItem> Action { get; set; }
    }
}