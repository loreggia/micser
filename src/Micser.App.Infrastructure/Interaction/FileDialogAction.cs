using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using Prism.Interactivity.InteractionRequest;
using System.Windows;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// Base class for open/save file dialog actions.
    /// </summary>
    public abstract class FileDialogAction : TriggerAction<FrameworkElement>
    {
        /// <summary>
        /// Creates the dialog instance.
        /// </summary>
        /// <returns></returns>
        protected abstract FileDialog CreateDialog();

        /// <summary>
        /// Initializes the dialog with values from the <see cref="FileDialogConfirmation"/> request.
        /// </summary>
        protected virtual void InitDialog(FileDialog dialog, FileDialogConfirmation dialogConfirmation)
        {
            dialog.DefaultExt = dialogConfirmation.DefaultExtension;
            dialog.Filter = dialogConfirmation.Filter;
        }

        /// <inheritdoc />
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