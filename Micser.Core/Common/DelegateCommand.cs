using System;
using System.Windows.Input;

namespace Micser.Core.Common
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _action;
        private readonly Func<object, bool> _canExecuteFunc;

        public DelegateCommand(Action<object> action, Func<object, bool> canExecuteFunc = null)
        {
            _action = action;
            _canExecuteFunc = canExecuteFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecuteFunc?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _action.Invoke(parameter);
        }
    }
}