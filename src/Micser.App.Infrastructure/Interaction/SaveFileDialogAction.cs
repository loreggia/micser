using Microsoft.Win32;

namespace Micser.App.Infrastructure.Interaction
{
    public class SaveFileDialogAction : FileDialogAction
    {
        protected override FileDialog CreateDialog()
        {
            return new SaveFileDialog();
        }
    }
}