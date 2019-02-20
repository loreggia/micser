using System;
using System.Windows.Input;

namespace Micser.App.Infrastructure
{
    public abstract class ViewModel : Bindable, IViewModel, IDisposable
    {
        private bool _isBusy;

        protected ViewModel()
        {
            CommandBindings = new CommandBindingCollection();
        }

        public CommandBindingCollection CommandBindings { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        protected void AddCommandBinding(RoutedUICommand applicationCommand, ICommand vmCommand)
        {
            CommandBindings.Add(new CommandBindingToCommand(applicationCommand, vmCommand));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CommandBindings.Clear();
            }
        }
    }
}