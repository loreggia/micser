using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Interaction
{
    /// <summary>
    /// Open/save file dialog confirmation for use with an <see cref="InteractionRequest{IConfirmation}"/>.
    /// </summary>
    public class FileDialogConfirmation : Confirmation
    {
        private readonly Dictionary<string, string[]> _filters;

        /// <inheritdoc />
        public FileDialogConfirmation()
        {
            _filters = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Gets or sets the default file extension.
        /// </summary>
        public string DefaultExtension { get; set; }

        /// <summary>
        /// Gets the processed filter string.
        /// </summary>
        public string Filter => string.Join("|", _filters.Select(f => f.Key + "|" + string.Join(";", f.Value)));

        /// <summary>
        /// Adds a file type filter.
        /// </summary>
        /// <param name="text">The text to show for this filter in the drop down.</param>
        /// <param name="extensions">The file extension (i.e. "*.json")</param>
        public void AddFilter(string text, params string[] extensions)
        {
            _filters.Add(text, extensions);
        }
    }
}