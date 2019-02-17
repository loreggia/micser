using System;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Menu
{
    public class MenuItemDescription
    {
        private bool _isSeparator;
        public ICommand Command { get; set; }
        public string Header { get; set; }
        public string IconResourceName { get; set; }
        public string Id { get; set; }

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

        public int Order { get; set; }
        public string ParentId { get; set; }
    }
}