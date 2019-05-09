using System;
using System.Windows.Input;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Base class for UI component view models.
    /// </summary>
    public abstract class ViewModel : Bindable, IViewModel, IDisposable
    {
        private bool _isBusy;

        protected ViewModel()
        {
            CommandBindings = new CommandBindingCollection();
        }

        /// <summary>
        /// Gets a collection of command bindings. Use <see cref="AddCommandBinding"/> to add a link between a WPF <see cref="RoutedUICommand"/> and an <see cref="ICommand"/>.
        /// </summary>
        public CommandBindingCollection CommandBindings { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether the view model is busy and the view should show a loading state.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets a value indicating whether this view model instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Links a WPF <see cref="RoutedUICommand"/> to a <see cref="ICommand"/> using a <see cref="CommandBindingToCommand"/>.
        /// </summary>
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