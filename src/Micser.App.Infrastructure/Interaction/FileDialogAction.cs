using Microsoft.Win32;
using Prism.Interactivity.InteractionRequest;
using System.Windows;
using System.Windows.Interactivity;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// Base class for open/save file dialog actions.
    /// </summary>
    public abstract class FileDialogAction : TriggerAction<FrameworkElement>
    {
        protected abstract FileDialog CreateDialog();

        protected virtual void InitDialog(FileDialog dialog, FileDialogConfirmation dialogConfirmation)
        {
            dialog.DefaultExt = dialogConfirmation.DefaultExtension;
            dialog.Filter = dialogConfirmation.Filter;
        }

        protected override void Invoke(object parameter)
        {
            if (!(parameter is InteractionRequestedEventArgs args))
            {
                return;
            }

            var dialog = CreateDialog();
            dialog.Title = args.Context.Title;

            if (args.Context is FileDialogConfirmation dialogConfirmation)
            {
                InitDialog(dialog, dialogConfirmation);
            }

            var result = dialog.ShowDialog();

            if (args.Context is IConfirmation confirmation)
            {
                confirmation.Confirmed = result == true;
            }

            args.Context.Content = dialog.FileName;

            args.Callback();
        }
    }
}