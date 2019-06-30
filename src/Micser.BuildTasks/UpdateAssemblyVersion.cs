﻿using Microsoft.Build.Utilities;
using System.IO;
using System.Text.RegularExpressions;

namespace Micser.BuildTasks
{
    public class UpdateAssemblyVersion : Task
    {
        private readonly Regex _rxVersion;

        public UpdateAssemblyVersion()
        {
            _rxVersion = new Regex(@"(\d+)\.(\d+)\.(\d+).(\d+)", RegexOptions.Compiled);
        }

        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }

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

            var fileContent = File.ReadAllText(InputFileName);
        }
    }
}