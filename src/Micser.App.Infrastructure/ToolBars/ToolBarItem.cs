using System;

namespace Micser.App.Infrastructure.ToolBars
{
    public abstract class ToolBarItem
    {
        public string Description { get; set; }
        public Uri IconPath { get; set; }
        public string Name { get; set; }
    }
}