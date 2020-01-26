using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Interaction
{
    public class FileDialogOptions
    {
        private readonly Dictionary<string, string[]> _filters;

        public FileDialogOptions(string title, string defaultExtension, string fileName = null)
            : this()
        {
            Title = title;
            DefaultExtension = defaultExtension;
            FileName = fileName;
        }

        public FileDialogOptions()
        {
            _filters = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Gets or sets the default file extension.
        /// </summary>
        public string DefaultExtension { get; set; }

        /// <summary>
        /// Gets or sets the default file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets the processed filter string.
        /// </summary>
        public string Filter => string.Join("|", _filters.Select(f => f.Key + "|" + string.Join(";", f.Value)));

        /// <summary>
        /// Gets or sets the dialog title.
        /// </summary>
        public string Title { get; set; }

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