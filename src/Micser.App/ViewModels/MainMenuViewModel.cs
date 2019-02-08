using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Menu;
using System.Collections.Generic;

namespace Micser.App.ViewModels
{
    public class MainMenuViewModel : ViewModelNavigationAware
    {
        private readonly IMenuItemRegistry _menuItemRegistry;
        private IEnumerable<TreeNode<MenuItemDescription>> _menuItems;

        public MainMenuViewModel(IMenuItemRegistry menuItemRegistry)
        {
            _menuItemRegistry = menuItemRegistry;
        }

        public IEnumerable<TreeNode<MenuItemDescription>> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        protected override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            var menuItems = _menuItemRegistry.Items;
            var tree = TreeNode<MenuItemDescription>.CreateTree(menuItems, m => m.Id, m => m.ParentId, m => m.Order);
            MenuItems = tree;
        }
    }
}