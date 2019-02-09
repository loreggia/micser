using Micser.Common;

namespace Micser.App.Infrastructure.ToolBars
{
    public interface IToolBarRegistry : IItemRegistry<ToolBarDescription>
    {
        void AddItem(string toolBarName, ToolBarItem item);

        ToolBarDescription GetToolBar(string toolBarName);
    }
}