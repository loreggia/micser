using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Micser.Common;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Micser.Engine.Infrastructure.Updates
{
    public class AzureUpdateService : IUpdateService
    {
        private readonly ILogger _logger;

        public AzureUpdateService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> DownloadInstaller(UpdateManifest manifest)
        {
            if (manifest == null)
            {
                return null;
            }

            var rootDirectory = GetUpdateDirectory();
            var msiDirectory = rootDirectory.GetDirectoryReference("msi");
            var updateFile = msiDirectory.GetFileReference(manifest.FileName);

            if (!await updateFile.ExistsAsync())
            {
                return null;
            }

            var targetPath = Path.Combine(Globals.AppDataFolder, "msi", manifest.FileName);
            await updateFile.DownloadToFileAsync(targetPath, FileMode.Create);
            return targetPath;
        }

        public async Task<UpdateManifest> GetUpdateManifest()
        {
            var rootDirectory = GetUpdateDirectory();
            var manifestFile = rootDirectory.GetFileReference("manifest.json");

            if (!await manifestFile.ExistsAsync())
            {
                return null;
            }

            using (var manifestStream = await manifestFile.OpenReadAsync())
            using (var streamReader = new StreamReader(manifestStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<UpdateManifest>(jsonReader);
            }
        }

        private CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            try
            {
                return CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return null;
            }
        }

        private CloudFileDirectory GetUpdateDirectory()
        {
            var connectionString = "BlobEndpoint=https://micser.blob.core.windows.net/;QueueEndpoint=https://micser.queue.core.windows.net/;FileEndpoint=https://micser.file.core.windows.net/;TableEndpoint=https://micser.table.core.windows.net/;SharedAccessSignature=sv=2018-03-28&ss=f&srt=sco&sp=rl&se=9999-12-31T00:00:00Z&st=2019-06-14T03:43:37Z&spr=https&sig=VttEwElW2as22PoOKxNBiN%2FtnkSgJknhY5WbXnNpV3c%3D";
            //var connectionString = "UseDevelopmentStorage=true";
            var shareName = "updates";

            var storageAccount = CreateStorageAccountFromConnectionString(connectionString);

            if (storageAccount == null)
            {
                return null;
            }

            var fileClient = storageAccount.CreateCloudFileClient();
            var fileShare = fileClient.GetShareReference(shareName);
            return fileShare.GetRootDirectoryReference();
        }
    }
}