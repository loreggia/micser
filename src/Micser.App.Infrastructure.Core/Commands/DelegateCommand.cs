using System;
using System.Linq.Expressions;

namespace Micser.App.Infrastructure.Commands
{
    public class DelegateCommand : DelegateCommandBase
    {
        private readonly Func<bool> _canExecuteFunc;
        private readonly Action _executeAction;

        public DelegateCommand(Action executeAction)
            : this(executeAction, () => true)
        {
        }

        public DelegateCommand(Action executeAction, Func<bool> canExecuteFunc)
        {
            _executeAction = executeAction;
            _canExecuteFunc = canExecuteFunc;
        }

        public DelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
        {
            ObservesPropertyInternal(propertyExpression);
            return this;
        }

        protected override bool CanExecute(object parameter)
        {
            return _canExecuteFunc();
        }

        protected override void Execute(object parameter)
        {
            _executeAction();
        }
    }

    public class DelegateCommand<T> : DelegateCommandBase
    {
        private readonly Func<T, bool> _canExecuteFunc;
        private readonly Action<T> _executeAction;

        public DelegateCommand(Action<T> executeAction)
            : this(executeAction, _ => true)
        {
        }

        public DelegateCommand(Action<T> executeAction, Func<T, bool> canExecuteFunc)
        {
            _executeAction = executeAction;
            _canExecuteFunc = canExecuteFunc;
        }

        public DelegateCommand<T> ObservesProperty<TProperty>(Expression<Func<TProperty>> propertyExpression)
        {
            ObservesPropertyInternal(propertyExpression);
            return this;
        }

        protected override bool CanExecute(object parameter)
        {
            return _canExecuteFunc((T)parameter);
        }

        protected override void Execute(object parameter)
        {
            _executeAction((T)parameter);
        }
    }
}