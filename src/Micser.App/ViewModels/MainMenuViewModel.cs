using System;
using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Menu;
using System.Collections.Generic;

namespace Micser.App.ViewModels
{
    public class MainMenuViewModel : ViewModelNavigationAware
    {
        private readonly IMenuItemRegistry _menuItemRegistry;
        private IEnumerable<TreeNode<MenuItemDescription>> _menuItems;

        public MainMenuViewModel()
        {
            var menuItems = new List<MenuItemDescription>()
            {
                new MenuItemDescription
                {
                    Header = "Header",
                    Id = "Top"
                },
                new MenuItemDescription
                {
                    Header = "Sub 1",
                    Id = "Sub1",
                    ParentId = "Top",
                    Order = 1
                },
                new MenuItemDescription
                {
                    Header = "Separator",
                    IsSeparator = true,
                    ParentId = "Top",
                    Order = 2
                },
                new MenuItemDescription
                {
                    Header = "Sub 2",
                    Id = "Sub2",
                    ParentId = "Top",
                    Order = 3
                },
                new MenuItemDescription
                {
                    Header = "Sub 2 Sub 1",
                    Id = "Sub2Sub1",
                    ParentId = "Top",
                    Order = 1
                }
            };

            MenuItems = CreateTree(menuItems);
        }

        public MainMenuViewModel(IMenuItemRegistry menuItemRegistry)
        {
            _menuItemRegistry = menuItemRegistry;
        }

        public IEnumerable<TreeNode<MenuItemDescription>> MenuItems
        {
            get => _menuItems;
            set => SetProperty(ref _menuItems, value);
        }

        private static IEnumerable<TreeNode<MenuItemDescription>> CreateTree(IEnumerable<MenuItemDescription> menuItems)
        {
            return TreeNode<MenuItemDescription>.CreateTree(menuItems, m => m.Id, m => m.ParentId, m => m.Order);
        }

        protected override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);

            var menuItems = _menuItemRegistry.Items;
            MenuItems = CreateTree(menuItems);
        }
    }
}