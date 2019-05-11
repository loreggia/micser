using System.Windows;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <summary>
    /// Specifies a tool bar item's placement mode.
    /// </summary>
    public enum ToolBarItemPlacement
    {
        /// <summary>
        /// The item is always shown in the tool bar.
        /// </summary>
        ToolBar,

        /// <summary>
        /// The item is always shown in the overflow panel.
        /// </summary>
        Overflow,

        /// <summary>
        /// The item is shown in the tool bar if there is enough space, otherwise it is moved to the overflow panel.
        /// </summary>
        Auto
    }

    /// <summary>
    /// Base class for tool bar items.
    /// </summary>
    public abstract class ToolBarItem
    {
        /// <inheritdoc />
        protected ToolBarItem()
        {
            Placement = ToolBarItemPlacement.ToolBar;
        }

        /// <summary>
        /// Gets or sets the item's description that is shown as a tool tip.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name/key of a <see cref="DataTemplate"/> resource to use as the item's icon.
        /// </summary>
        public string IconTemplateName { get; set; }

        /// <summary>
        /// Gets or sets the name/header of the item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the order amongst the tool bar items in the same tool bar.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the item's default placement. Default is <see cref="ToolBarItemPlacement.ToolBar"/>.
        /// </summary>
        public ToolBarItemPlacement Placement { get; set; }
    }
}