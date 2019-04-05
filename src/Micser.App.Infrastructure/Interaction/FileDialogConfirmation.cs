using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using System.Linq;

namespace Micser.App.Infrastructure.Interaction
{
    public class FileDialogConfirmation : Confirmation
    {
        private readonly Dictionary<string, string[]> _filters;

        public FileDialogConfirmation()
        {
            _filters = new Dictionary<string, string[]>();
        }

        public string DefaultExtension { get; set; }

        public string Filter => string.Join("|", _filters.Select(f => f.Key + "|" + string.Join(";", f.Value)));

        public void AddFilter(string text, params string[] extensions)
        {
            _filters.Add(text, extensions);
        }
    }
}