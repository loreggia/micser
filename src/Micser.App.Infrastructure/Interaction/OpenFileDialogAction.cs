using Microsoft.Win32;

namespace Micser.App.Infrastructure.Interaction
{
    public class OpenFileDialogAction : FileDialogAction
    {
        protected override FileDialog CreateDialog()
        {
            return new OpenFileDialog();
        }
    }
}