using Prism.Commands;
using System;
using System.Windows.Input;

namespace Micser.App.Infrastructure.ToolBars
{
    public class ToolBarButton : ToolBarItem
    {
        private Action<ToolBarItem> _action;
        private ICommand _actionCommand;

        public Action<ToolBarItem> Action
        {
            get => _action;
            set
            {
                _action = value;
                _actionCommand = null;
            }
        }

        public ICommand ActionCommand => _actionCommand ?? (_actionCommand = new DelegateCommand(() => Action(this)));
    }
}