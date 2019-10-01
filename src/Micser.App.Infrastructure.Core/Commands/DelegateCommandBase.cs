using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;

namespace Micser.App.Infrastructure.Commands
{
    public abstract class DelegateCommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged;

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        public virtual void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract bool CanExecute(object parameter);

        protected abstract void Execute(object parameter);

        protected virtual void ObservesPropertyInternal(Expression propertyExpression)
        {
            var propNameStack = new Stack<PropertyInfo>();

            while (propertyExpression is MemberExpression temp)
            {
                propertyExpression = temp.Expression;
                propNameStack.Push(temp.Member as PropertyInfo);
            }

            if (!(propertyExpression is ConstantExpression constantExpression))
            {
                throw new NotSupportedException();
            }

            var propOwnerObject = constantExpression.Value;

            if (!(propOwnerObject is INotifyPropertyChanged inpcObject))
            {
                throw new InvalidOperationException();
            }

            inpcObject.PropertyChanged += OnObservedPropertyChanged;
        }

        private void OnObservedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseCanExecuteChanged();
        }
    }
}