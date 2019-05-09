using Micser.Common;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <summary>
    /// Registry containing all tool bar items.
    /// </summary>
    public interface IToolBarRegistry : IItemRegistry<ToolBarDescription>
    {
        /// <summary>
        /// Adds a tool bar item to the registry.
        /// </summary>
        /// <param name="toolBarName">The name of the tool bar.</param>
        /// <param name="item">The tool bar item.</param>
        void AddItem(string toolBarName, ToolBarItem item);

        /// <summary>
        /// Gets a <see cref="ToolBarDescription"/> containing it's items.
        /// </summary>
        /// <param name="toolBarName">The name of the tool bar.</param>
        ToolBarDescription GetToolBar(string toolBarName);
    }
}