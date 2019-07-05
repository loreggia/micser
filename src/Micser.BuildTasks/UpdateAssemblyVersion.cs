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
        private const string VersionGroupName = "version";
        private readonly Regex _rxAssemblyVersion;

        public UpdateAssemblyVersion()
        {
            _rxAssemblyVersion = new Regex($@"Assembly(File)?Version\s*\(\s*""(?<{VersionGroupName}>[^""]+)""\s*\)", RegexOptions.Compiled);
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

            if (!File.Exists(OutputFileName))
            {
                Log.LogError($"Output file '{OutputFileName}' not found.");
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
                var inputContent = File.ReadAllText(InputFileName);
                var output = _rxAssemblyVersion.ReplaceGroup(inputContent, VersionGroupName, versionContent);
                File.WriteAllText(OutputFileName, output);

                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}