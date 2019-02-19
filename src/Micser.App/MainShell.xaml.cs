using Micser.App.Infrastructure;
using Micser.App.Infrastructure.Settings;
using Prism.Events;
using Prism.Regions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Micser.App
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainShell
    {
        private readonly IRegionManager _regionManager;
        private bool _isExiting;

        public MainShell(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            InitializeComponent();

            _regionManager = regionManager;
            eventAggregator.GetEvent<NavigationEvent>().Subscribe(OnNavigated);

            Loaded += MainShell_Loaded;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var settingsService = Application.GetService<ISettingsService>();
            _isExiting = settingsService.GetSetting<bool>(AppGlobals.SettingKeys.ExitOnClose);

            e.Cancel = !_isExiting;
            base.OnClosing(e);
            if (!_isExiting)
            {
                Hide();
            }
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _isExiting = true;
            _regionManager.RequestNavigate(AppGlobals.PrismRegions.Main, "");
            System.Windows.Application.Current.Shutdown();
        }

        private void MainShell_Loaded(object sender, RoutedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnNavigated(NavigationInfo info)
        {
            if (info.RegionName == AppGlobals.PrismRegions.Main)
            {
                (MainRegion.Content as IInputElement)?.Focus();
            }
        }

        private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            CustomApplicationCommands.Restore.Execute(null, TaskbarIcon);
        }

        private void RestoreCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Show();
        }

        private void TaskbarIconExitClick(object sender, RoutedEventArgs e)
        {
            CustomApplicationCommands.Exit.Execute(null, TaskbarIcon);
        }

        private void TaskbarIconRestoreClick(object sender, RoutedEventArgs e)
        {
            // using application commands doesn't work in the taskbar context menu
            CustomApplicationCommands.Restore.Execute(null, TaskbarIcon);
        }
    }
}