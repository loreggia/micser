using Prism.Regions;

namespace Micser.App.Infrastructure
{
    public class ViewModelNavigationAware : ViewModel, INavigationAware
    {
        public bool DisposeOnNavigatedFrom { get; set; }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (DisposeOnNavigatedFrom)
            {
                Dispose();
            }
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }
    }
}