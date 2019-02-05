using CommonServiceLocator;
using Prism.Commands;

namespace Micser.App.Infrastructure
{
    public class NavigationCommand<TView> : DelegateCommand
    {
        public NavigationCommand()
            : base(OnCommandExecuted)
        {
        }

        private static void OnCommandExecuted()
        {
            var navigationManager = ServiceLocator.Current.GetInstance<INavigationManager>();
            navigationManager.Navigate<TView>();
        }
    }
}