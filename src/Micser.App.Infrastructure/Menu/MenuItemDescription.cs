using System;
using System.Windows;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Menu
{
    /// <summary>
    /// Describes a menu item.
    /// </summary>
    public class MenuItemDescription
    {
        private bool _isSeparator;

        /// <summary>
        /// Gets or sets the command to execute when the item is clicked.
        /// </summary>
        public ICommand Command { get; set; }

        /// <summary>
        /// Gets or sets the text that is displayed on this item.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets the resource name of the <see cref="DataTemplate"/> to use as icon.
        /// </summary>
        public string IconTemplateName { get; set; }

        /// <summary>
        /// Gets or sets the item's ID. This needs to be set if <see cref="IsSeparator"/> is false.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value whether the item is a separator.
        /// Setting this will set the <see cref="Id"/> property if it is empty.
        /// </summary>
        public bool IsSeparator
        {
            get => _isSeparator;
            set
            {
                _isSeparator = value;
                if (value && string.IsNullOrEmpty(Id))
                {
                    Id = Guid.NewGuid().ToString();
                }
            }
        }

        /// <summary>
        /// The order amongst the item's siblings.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The <see cref="Id"/> of the item's parent.
        /// </summary>
        public string ParentId { get; set; }
    }
}