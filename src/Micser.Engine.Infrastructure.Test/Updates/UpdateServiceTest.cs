using Micser.Common.Test;
using Micser.Common.Updates;
using NLog;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Engine.Infrastructure.Test.Updates
{
    public class UpdateServiceTest
    {
        public UpdateServiceTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelperTarget.ConfigureLogger(testOutputHelper);
        }

        [Fact]
        public async Task GetUpdateManifest()
        {
            var service = new HttpUpdateService(GetSettings(), LogManager.GetLogger("LocalUpdateService"));

            var manifest = await service.GetUpdateManifestAsync();

            Assert.NotNull(manifest);
        }

        [Fact]
        public async Task GetUpdateManifestAndDownloadInstaller()
        {
            var service = new HttpUpdateService(GetSettings(), LogManager.GetLogger("LocalUpdateService"));

            var manifest = await service.GetUpdateManifestAsync();

            Assert.NotNull(manifest);

            var fileName = await service.DownloadInstallerAsync(manifest);

            Assert.NotNull(fileName);
            Assert.True(File.Exists(fileName));

            File.Delete(fileName);
        }

        private HttpUpdateSettings GetSettings()
        {
            return new HttpUpdateSettings { ManifestUrl = "https://micser.lloreggia.ch/update/manifest.json" };
        }
    }
}