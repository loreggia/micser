using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Micser.App.Infrastructure
{
    public class IniFile
    {
        private static readonly Regex RxSection;
        private static readonly Regex RxValue;
        private readonly string _fileName;
        private Dictionary<string, Dictionary<string, string>> _values;

        static IniFile()
        {
            RxSection = new Regex(@"^\[(?<section>\w+)\]", RegexOptions.Compiled);
            RxValue = new Regex(@"^(?<key>\w+)\W*=\W*('|"")?(?<value>[^'""]+)('|"")?", RegexOptions.Compiled);
        }

        public IniFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("Ini file not found.", fileName);
            }

            _values = new Dictionary<string, Dictionary<string, string>>();
            _fileName = fileName;
            Load();
        }

        public string GetValue(string section, string key)
        {
            if (_values.TryGetValue(section.ToLower(), out var sectionDict) && sectionDict.TryGetValue(key.ToLower(), out var value))
            {
                return value;
            }

            return null;
        }

        private void Load()
        {
            _values.Clear();

            using (var reader = new StreamReader(_fileName))
            {
                string line, currentSection = "default";
                while ((line = reader.ReadLine()) != null)
                {
                    var sectionMatch = RxSection.Match(line);
                    if (sectionMatch.Success && sectionMatch.Groups["section"]?.Value is string section)
                    {
                        currentSection = section.ToLower();

                        if (!_values.ContainsKey(currentSection))
                        {
                            _values.Add(currentSection, new Dictionary<string, string>());
                        }

                        continue;
                    }

                    var valueMatch = RxValue.Match(line);
                    if (valueMatch.Success && valueMatch.Groups["key"]?.Value is string key)
                    {
                        _values[currentSection][key.ToLower()] = valueMatch.Groups["value"]?.Value;
                    }
                }
            }
        }
    }
}