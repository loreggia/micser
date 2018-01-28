﻿using System.Collections.Generic;
using Micser.Infrastructure.Menu;
using Prism.Regions;

namespace Micser.Infrastructure.ViewModels
{
    public class MainMenuViewModel : ViewModel
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

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var menuItems = _menuItemRegistry.Items;
            var tree = TreeNode<MenuItemDescription>.CreateTree(menuItems, m => m.Id, m => m.ParentId, m => m.Order);
            MenuItems = tree;
        }
    }
}