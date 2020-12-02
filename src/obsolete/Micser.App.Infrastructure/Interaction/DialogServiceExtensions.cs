using Microsoft.Win32;
using Prism.Services.Dialogs;
using System;
using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    public static class DialogServiceExtensions
    {
        public static void ShowMessageBox(this IDialogService dialogService, string message, MessageBoxButton buttons, MessageBoxImage image, Action<ButtonResult> resultCallback)
        {
            var parameters = new DialogParameters
            {
                {"message", message},
                {"image", image},
                {"buttons", buttons}
            };
            dialogService.ShowDialog(typeof(MessageBoxView).Name, parameters, result => resultCallback(result.Result));
        }

        public static bool ShowOpenFileDialog(this IDialogService dialogService, FileDialogOptions options, out string fileName)
        {
            var dialog = new OpenFileDialog();

            ApplyFileDialogOptions(dialog, options);

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                return true;
            }

            fileName = null;
            return false;
        }

        public static bool ShowSaveFileDialog(this IDialogService dialogService, FileDialogOptions options, out string fileName)
        {
            var dialog = new SaveFileDialog();

            ApplyFileDialogOptions(dialog, options);

            if (dialog.ShowDialog() == true)
            {
                fileName = dialog.FileName;
                return true;
            }

            fileName = null;
            return false;
        }

        private static void ApplyFileDialogOptions(FileDialog dialog, FileDialogOptions options)
        {
            dialog.Title = options.Title;
            dialog.DefaultExt = options.DefaultExtension;
            dialog.Filter = options.Filter;
            dialog.FileName = options.FileName;
        }
    }
}