using Micser.App.Infrastructure;
using Micser.Common.Extensions;
using Micser.Common.Settings;
using Prism.Regions;
using System;
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

        public MainShell(IRegionManager regionManager)
        {
            InitializeComponent();

            _regionManager = regionManager;

            Loaded += MainShell_Loaded;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
            Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                // ISettingsService is not yet available when the shell is created, so we can't use constructor injection
                var settingsService = MicserApplication.GetService<ISettingsService>();
                if (settingsService.GetSetting<bool>(AppGlobals.SettingKeys.MinimizeToTray))
                {
                    Hide();
                }
            }
            base.OnStateChanged(e);
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Shutdown();
        }

        private void MainShell_Loaded(object sender, RoutedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            CustomApplicationCommands.Restore.Execute(null, TaskbarIcon);
        }

        private void RestoreCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Show();

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Shutdown()
        {
            _regionManager.RequestNavigate(AppGlobals.PrismRegions.Main, "");
            Application.Current.Shutdown();
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