using System;

namespace Micser.App.Infrastructure.ToolBars
{
    public enum ToolBarItemPlacement
    {
        ToolBar,
        Overflow,
        Auto
    }

    public abstract class ToolBarItem
    {
        protected ToolBarItem()
        {
            Placement = ToolBarItemPlacement.ToolBar;
        }

        public string Description { get; set; }
        public Uri IconPath { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public ToolBarItemPlacement Placement { get; set; }
    }
}