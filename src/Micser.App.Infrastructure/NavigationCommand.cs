using CommonServiceLocator;
using Prism.Commands;

namespace Micser.App.Infrastructure
{
    public class NavigationCommand<TView> : DelegateCommandBase
    {
        public NavigationCommand(string regionName, object parameter = null)
        {
            RegionName = regionName;
            Parameter = parameter;
        }

        public object Parameter { get; }
        public string RegionName { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            var navigationManager = ServiceLocator.Current.GetInstance<INavigationManager>();
            navigationManager.Navigate<TView>(RegionName, Parameter);
        }
    }
}