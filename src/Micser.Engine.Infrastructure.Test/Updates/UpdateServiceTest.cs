using Micser.Common.Test;
using Micser.Engine.Infrastructure.Updates;
using NLog;
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
            //var service = new AzureUpdateService(LogManager.GetCurrentClassLogger());
            var service = new LocalUpdateService(LogManager.GetLogger("LocalUpdateService"));

            var manifest = await service.GetUpdateManifestAsync();

            Assert.NotNull(manifest);
        }
    }
}