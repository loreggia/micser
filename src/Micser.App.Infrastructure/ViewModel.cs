using System;

namespace Micser.App.Infrastructure
{
    public abstract class ViewModel : Bindable, IViewModel, IDisposable
    {
        private bool _isBusy;

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

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}