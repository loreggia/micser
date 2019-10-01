using System;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Commands
{
    public abstract class DelegateCommandBase : ICommand
    {
        private readonly Action<object> _action;
        private readonly Func<object, bool> _canExecute;

        protected DelegateCommandBase()
        {
        }

        protected DelegateCommandBase(Action<object> action, Func<object, bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            throw new NotImplementedException();
        }

        protected virtual bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        protected virtual void Execute(object parameter)
        {
            _action?.Invoke(parameter);
        }

        protected virtual void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}