using Prism.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Micser.App.Infrastructure
{
    public class AsyncDelegateCommand : DelegateCommandBase, IDisposable
    {
        private readonly Func<Task<bool>> _canExecuteMethod;
        private readonly Func<Task> _executeMethod;
        private bool _canExecuteState;
        private Timer _timer;

        public AsyncDelegateCommand(Func<Task> executeMethod, Func<Task<bool>> canExecuteMethod, int checkInterval = 500)
        {
            CheckInterval = checkInterval;
            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;

            _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(checkInterval));
        }

        public int CheckInterval { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override bool CanExecute(object parameter)
        {
            return _canExecuteState;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

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