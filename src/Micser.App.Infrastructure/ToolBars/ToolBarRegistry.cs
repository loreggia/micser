using System.Collections.Generic;

namespace Micser.App.Infrastructure.ToolBars
{
    /// <inheritdoc cref="IToolBarRegistry"/>
    public class ToolBarRegistry : IToolBarRegistry
    {
        private readonly Dictionary<string, ToolBarDescription> _toolBars;

        public ToolBarRegistry()
        {
            _toolBars = new Dictionary<string, ToolBarDescription>();
        }

        public IEnumerable<ToolBarDescription> Items => _toolBars.Values;

        public void Add(ToolBarDescription toolBar)
        {
            if (_toolBars.ContainsKey(toolBar.Name))
            {
                foreach (var item in toolBar.Items)
                {
                    _toolBars[toolBar.Name].Add(item);
                }
            }
            else
            {
                _toolBars.Add(toolBar.Name, toolBar);
            }
        }

        public void AddItem(string toolBarName, ToolBarItem item)
        {
            if (!_toolBars.ContainsKey(toolBarName))
            {
                _toolBars.Add(toolBarName, new ToolBarDescription());
            }

            _toolBars[toolBarName].Add(item);
        }

        public ToolBarDescription GetToolBar(string toolBarName)
        {
            return _toolBars.ContainsKey(toolBarName) ? _toolBars[toolBarName] : null;
        }
    }
}