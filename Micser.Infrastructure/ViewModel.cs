using System;

namespace Micser.Infrastructure
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