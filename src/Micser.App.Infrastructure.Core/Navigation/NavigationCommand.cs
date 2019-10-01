using Micser.App.Infrastructure.Commands;

namespace Micser.App.Infrastructure.Navigation
{
    /// <summary>
    /// A delegate command that navigates to a specific view when executed.
    /// </summary>
    public class NavigationCommand<TView> : DelegateCommandBase
    {
        private readonly INavigationManager _navigationManager;

        /// <summary>
        /// Creates an instance of the <see cref="NavigationCommand{TView}"/> class.
        /// </summary>
        /// <param name="navigationManager">The navigation manager.</param>
        /// <param name="regionName">The region to perform the navigation in.</param>
        /// <param name="parameter">The parameter object to pass to the navigation call.</param>
        public NavigationCommand(INavigationManager navigationManager, string regionName, object parameter = null)
        {
            _navigationManager = navigationManager;
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
            _navigationManager.Navigate<TView>(RegionName, Parameter);
        }
    }
}