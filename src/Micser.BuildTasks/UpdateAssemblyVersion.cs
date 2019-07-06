using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Micser.Common.Extensions;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Micser.BuildTasks
{
    public class UpdateAssemblyVersion : Task
    {
        private readonly Regex _rxAssemblyVersion;

        public UpdateAssemblyVersion()
        {
            _rxAssemblyVersion = new Regex(@"Assembly(File)?Version\s*\(\s*""(?<version>[^""]+)""\s*\)", RegexOptions.Compiled);
        }

        [Required]
        public string InputFileName { get; set; }

        [Required]
        public string OutputFileName { get; set; }

        [Required]
        public string VersionFileName { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(InputFileName))
            {
                Log.LogError($"Input file '{InputFileName}' not found.");
                return false;
            }

            if (!File.Exists(VersionFileName))
            {
                Log.LogError($"Version file '{VersionFileName}' not found.");
                return false;
            }

            var versionContent = File.ReadAllText(VersionFileName);
            versionContent = versionContent.Trim();

            if (string.IsNullOrWhiteSpace(versionContent))
            {
                Log.LogError($"Invalid version file content ({VersionFileName}).");
                return false;
            }

            try
            {
                var version = ProcessVersion(versionContent);

                var inputContent = File.ReadAllText(InputFileName);
                var output = _rxAssemblyVersion.ReplaceGroup(inputContent, "version", version);
                File.WriteAllText(OutputFileName, output);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        private static string ProcessVersion(string versionContent)
        {
            var result = new ushort[4];

            var parts = versionContent.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (!int.TryParse(part, out var iValue) &&
                    !int.TryParse(DateTime.Now.ToString(part), out iValue))
                {
                    iValue = 0;
                }

                var maxValue = i < 2 ? 255 : 65535;

                if (iValue > maxValue)
                {
                    throw new InvalidOperationException($"The value {iValue} is too big to use as a version number (index: {i}, max: {maxValue}).");
                }

                result[i] = (ushort)iValue;
            }

            return string.Join(".", result);
        }
    }
}