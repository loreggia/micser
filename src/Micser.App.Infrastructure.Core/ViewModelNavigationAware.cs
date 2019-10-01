using Micser.App.Infrastructure.Navigation;

namespace Micser.App.Infrastructure
{
    /// <summary>
    /// Base class for view models that handle navigation events.
    /// </summary>
    public abstract class ViewModelNavigationAware : ViewModel, INavigationAware
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the view model should be disposed when navigating away.
        /// This will force the creation of a new view model when navigating to the corresponding view again.
        /// </summary>
        public bool DisposeOnNavigatedFrom { get; protected set; }

        /// <summary>
        /// Checks whether this instance qualifies as a navigation target and will be reused instead of creating a new instance upon navigation.
        /// Returns true unless the view model has been disposed.
        /// </summary>
        public virtual bool IsNavigationTarget(NavigationContext context)
        {
            return !IsDisposed;
        }

        void INavigationAware.OnNavigatedFrom(NavigationContext context)
        {
            OnNavigatedFrom(context.Parameter);

            if (DisposeOnNavigatedFrom)
            {
                Dispose();
            }
        }

        void INavigationAware.OnNavigatedTo(NavigationContext context)
        {
            OnNavigatedTo(context.Parameter);
        }

        /// <summary>
        /// Handler that is called after navigating away from the view.
        /// </summary>
        /// <param name="parameter">The parameter that is supplied to the <see cref="INavigationManager.Navigate{TView}"/> method while navigating to the new view.</param>
        protected virtual void OnNavigatedFrom(object parameter)
        {
        }

        /// <summary>
        /// Handler that is called after navigating to the view.
        /// </summary>
        /// <param name="parameter">The parameter that is supplied to the <see cref="INavigationManager.Navigate{TView}"/> method while navigating to this view.</param>
        protected virtual void OnNavigatedTo(object parameter)
        {
        }
    }
}