using System;
using Prism.Regions;

namespace Micser.Infrastructure
{
    public abstract class ViewModel : Bindable, IViewModel, IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}