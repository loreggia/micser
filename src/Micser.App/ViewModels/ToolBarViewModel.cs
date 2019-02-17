using Micser.App.Infrastructure;
using Micser.App.Infrastructure.ToolBars;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.ViewModels
{
    public class ToolBarViewModel : ViewModelNavigationAware
    {
        private readonly IToolBarRegistry _toolBarRegistry;
        private IEnumerable<ToolBarItem> _items;

        public ToolBarViewModel(IToolBarRegistry toolBarRegistry)
        {
            _toolBarRegistry = toolBarRegistry;
        }

        public IEnumerable<ToolBarItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        protected override void OnNavigatedTo(object parameter)
        {
            if (parameter is string toolBarName)
            {
                var toolBar = _toolBarRegistry.GetToolBar(toolBarName);

                if (toolBar != null)
                {
                    Items = toolBar.Items.OrderBy(i => i.Order).ToArray();
                }
            }

            base.OnNavigatedTo(parameter);
        }
    }
}