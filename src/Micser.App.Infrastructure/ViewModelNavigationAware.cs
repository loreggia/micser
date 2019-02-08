using Prism.Regions;

namespace Micser.App.Infrastructure
{
    public abstract class ViewModelNavigationAware : ViewModel, INavigationAware
    {
        public bool DisposeOnNavigatedFrom { get; protected set; }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return !IsDisposed;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext navigationContext)
        {
            OnNavigatedFrom(navigationContext.Parameters[AppGlobals.NavigationParameterKey]);

            if (DisposeOnNavigatedFrom)
            {
                Dispose();
            }
        }

        void INavigationAware.OnNavigatedTo(NavigationContext navigationContext)
        {
            OnNavigatedTo(navigationContext.Parameters[AppGlobals.NavigationParameterKey]);
        }

        protected virtual void OnNavigatedFrom(object parameter)
        {
        }

        protected virtual void OnNavigatedTo(object parameter)
        {
        }
    }
}