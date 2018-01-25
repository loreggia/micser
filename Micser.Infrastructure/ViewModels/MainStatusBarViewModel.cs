using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace Micser.Infrastructure.ViewModels
{
    public class MainStatusBarViewModel : ViewModel
    {
        private IEnumerable<StatusBarItem> _statusItems;

        public IEnumerable<StatusBarItem> StatusItems
        {
            get => _statusItems;
            set => SetProperty(ref _statusItems, value);
        }
    }
}