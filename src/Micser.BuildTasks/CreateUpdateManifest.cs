using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Micser.Common.Updates;
using System;
using System.IO;

namespace Micser.BuildTasks
{
    public class CreateUpdateManifest : Task
    {
        [Required]
        public string DescriptionFileName { get; set; }

        [Required]
        public string OutputFileName { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(DescriptionFileName))
            {
                Log.LogError($"The description file does not exist: {DescriptionFileName}.");
                return false;
            }

            var description = File.ReadAllText(DescriptionFileName);

            var updateManifest = new UpdateManifest
            {
                Date = DateTime.UtcNow,
                Description = description,
                FileName =
            }
        }
    }
}