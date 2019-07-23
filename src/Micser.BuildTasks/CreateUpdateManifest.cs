using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Micser.Common.Updates;
using Newtonsoft.Json;
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

        [Required]
        public string Version { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(DescriptionFileName))
            {
                Log.LogError($"The description file does not exist: {DescriptionFileName}.");
                return false;
            }

            try
            {
                var description = File.ReadAllText(DescriptionFileName);

                var updateManifest = new UpdateManifest
                {
                    Date = DateTime.UtcNow,
                    Description = description,
                    FileName = "",
                    Version = Version
                };

                var outputDir = Path.GetDirectoryName(OutputFileName);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.WriteAllText(OutputFileName, JsonConvert.SerializeObject(updateManifest));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }

            return true;
        }
    }
}