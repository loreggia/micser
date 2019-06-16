using Micser.Common.Test;
using Micser.Engine.Infrastructure.Updates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Engine.Infrastructure.Test
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
            var service = new LocalUpdateService();

            var manifest = await service.GetUpdateManifest();

            Assert.NotNull(manifest);
        }
    }
}