using System.Windows.Input;

namespace Micser.App.Infrastructure.Menu
{
    public class MenuItemDescription
    {
        public ICommand Command { get; set; }
        public string Header { get; set; }
        public string Id { get; set; }
        public bool IsSeparator { get; set; }
        public int Order { get; set; }
        public string ParentId { get; set; }
    }
}