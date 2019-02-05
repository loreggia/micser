﻿using Micser.App.Infrastructure;
using Micser.Common;
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
        private readonly ISettingsService _settingsService;
        private bool _isExiting;

        public MainShell(IRegionManager regionManager, ISettingsService settingsService)
        {
            _regionManager = regionManager;
            _settingsService = settingsService;

            InitializeComponent();

            Loaded += MainShell_Loaded;

            _settingsService.Load();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = !_isExiting;
            base.OnClosing(e);
            if (!_isExiting)
            {
                Hide();
            }
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _isExiting = _settingsService.GetSetting(Globals.SettingKeys.ExitOnClose, false);

            Close();
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _isExiting = true;
            _regionManager.RequestNavigate(Globals.PrismRegions.Main, "");
            _settingsService.Save();
            System.Windows.Application.Current.Shutdown();
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