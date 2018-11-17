using System;

namespace Micser.App.Infrastructure
{
    public abstract class ViewModel : Bindable, IViewModel, IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}