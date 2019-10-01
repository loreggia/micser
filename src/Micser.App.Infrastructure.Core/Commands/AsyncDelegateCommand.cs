using System;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure.Commands
{
    /// <summary>
    /// Similar to <see cref="DelegateCommand"/>, but can be supplied with async "execute" and "canExecute" methods. The "canExecute" method is periodically checked using a <see cref="Timer"/>.
    /// </summary>
    public class AsyncDelegateCommand : DelegateCommandBase, IDisposable
    {
        private readonly Func<Task<bool>> _canExecuteMethod;
        private readonly Func<Task> _executeMethod;
        private bool _canExecuteState;
        private Timer _timer;

        /// <summary>
        /// Creates an instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        /// <param name="executeMethod">The method to call when executing the command.</param>
        /// <param name="canExecuteMethod">The method to call when checking if the command can be executed.</param>
        /// <param name="checkInterval">The interval in milliseconds to periodically check if the command can be executed.</param>
        public AsyncDelegateCommand(Func<Task> executeMethod, Func<Task<bool>> canExecuteMethod, int checkInterval = 500)
        {
            CheckInterval = checkInterval;
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;

            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(checkInterval));
        }

        /// <summary>
        /// Gets the number of milliseconds that this command periodically checks its <see cref="CanExecute"/> method.
        /// </summary>
        public int CheckInterval { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        protected override bool CanExecute(object parameter)
        {
            return _canExecuteState;
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }

        /// <inheritdoc />
        protected override async void Execute(object parameter)
        {
            await _executeMethod();
        }

        private async void TimerCallback(object state)
        {
            var oldState = _canExecuteState;
            _canExecuteState = await _canExecuteMethod();
            if (oldState != _canExecuteState)
            {
                RaiseCanExecuteChanged();
            }
        }
    }
}