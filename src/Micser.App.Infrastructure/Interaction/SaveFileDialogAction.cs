using Microsoft.Win32;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// An interaction action showing a save file dialog.
    /// </summary>
    /// <example>
    /// &lt;in:Interaction.Triggers&gt;
    ///     &lt;ir:InteractionRequestTrigger SourceObject="{Binding SaveFileRequest}"&gt;
    ///         &lt; ii:SaveFileDialogAction /&gt;
    ///     &lt;/ir:InteractionRequestTrigger&gt;
    /// &lt;/in:Interaction.Triggers&gt;
    /// </example>
    public class SaveFileDialogAction : FileDialogAction
    {
        /// <inheritdoc />
        protected override FileDialog CreateDialog()
        {
            return new SaveFileDialog();
        }
    }
}