using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Micser.Common
{
    public class ArgumentDictionary
    {
        private readonly IList<string> _flags;
        private readonly string[] _nameChars;
        private readonly IDictionary<string, string> _namedValues;

        public ArgumentDictionary(params string[] args)
            : this(new[] { "-", "/", "--" }, args)
        {
        }

        public ArgumentDictionary(string[] nameChars, params string[] args)
        {
            _nameChars = nameChars;
            _namedValues = new Dictionary<string, string>();
            _flags = new List<string>();
            Initialize(args);
        }

        public string this[string arg]
        {
            get
            {
                arg = arg.ToLower(CultureInfo.InvariantCulture);
                return _namedValues.ContainsKey(arg) ? _namedValues[arg] : null;
            }
        }

        public bool HasFlag(string name)
        {
            return _flags.Contains(name.ToLower(CultureInfo.InvariantCulture));
        }

        public override string ToString()
        {
            var flags = _flags.Count > 0 ? "Flags: " + string.Join(", ", _flags) : null;
            var values = _namedValues.Count > 0 ? "Parameters: " + string.Join(", ", _namedValues.Select(p => $"[{p.Key}={p.Value}]")) : null;
            return string.Join(" | ", flags, values);
        }

        private void Initialize(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var nameChar = _nameChars.FirstOrDefault(c => args[i].StartsWith(c));
                if (nameChar != null)
                {
                    var name = args[i].Replace(nameChar, "").ToLower(CultureInfo.InvariantCulture);
                    if (i < args.Length - 1 && !_nameChars.Any(c => args[i + 1].StartsWith(c)))
                    {
                        _namedValues[name] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        _flags.Add(name);
                    }
                }
            }
        }
    }
}