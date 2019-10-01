namespace Micser.App.Infrastructure.Navigation
{
    public interface INavigationAware
    {
        bool IsNavigationTarget(NavigationContext context);

        void OnNavigatedFrom(NavigationContext context);

        void OnNavigatedTo(NavigationContext context);
    }
}