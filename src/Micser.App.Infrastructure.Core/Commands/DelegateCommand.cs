using System;

namespace Micser.App.Infrastructure.Commands
{
    public class DelegateCommand : DelegateCommandBase
    {
        public DelegateCommand(Action<object> action, Func<object, bool> canExecute)
            : base(action, canExecute)
        {
        }
    }
}