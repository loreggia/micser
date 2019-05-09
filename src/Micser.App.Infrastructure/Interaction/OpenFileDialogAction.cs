using Microsoft.Win32;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// An interaction action showing an open file dialog.
    /// </summary>
    /// <example>
    /// &lt;in:Interaction.Triggers&gt;
    ///     &lt;ir:InteractionRequestTrigger SourceObject="{Binding OpenFileRequest}"&gt;
    ///         &lt; ii:OpenFileDialogAction /&gt;
    ///     &lt;/ir:InteractionRequestTrigger&gt;
    /// &lt;/in:Interaction.Triggers&gt;
    /// </example>
    public class OpenFileDialogAction : FileDialogAction
    {
        protected override FileDialog CreateDialog()
        {
            return new OpenFileDialog();
        }
    }
}