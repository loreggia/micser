using Micser.App.Infrastructure.Commands;

namespace Micser.App.Infrastructure.Navigation
{
    /// <summary>
    /// A delegate command that navigates to a specific view when executed.
    /// </summary>
    public class NavigationCommand<TView> : DelegateCommandBase
    {
        /// <summary>
        /// Creates an instance of the <see cref="NavigationCommand{TView}"/> class.
        /// </summary>
        /// <param name="regionName">The region to perform the navigation in.</param>
        /// <param name="parameter">The parameter object to pass to the navigation call.</param>
        public NavigationCommand(string regionName, object parameter = null)
        {
            RegionName = regionName;
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the navigation parameter object.
        /// </summary>
        public object Parameter { get; }

        /// <summary>
        /// Gets the region name.
        /// </summary>
        public string RegionName { get; }

        /// <inheritdoc />
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        /// <inheritdoc />
        protected override void Execute(object parameter)
        {
            var navigationManager = ServiceLocator.Current.GetInstance<INavigationManager>();
            navigationManager.Navigate<TView>(RegionName, Parameter);
        }
    }
}