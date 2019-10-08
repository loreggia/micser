using Micser.Common.Updates;
using NLog;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Updates
{
    public class GitHubUpdateServiceTest
    {
        public GitHubUpdateServiceTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelperTarget.ConfigureLogger(testOutputHelper);
        }

        [Fact]
        public async Task GetUpdateManifest()
        {
            var service = new GitHubUpdateService(LogManager.GetCurrentClassLogger());

            var result = await service.GetUpdateManifestAsync();

            // TODO currently only for debugging purposes
            //Assert.NotNull(result);
            //Assert.NotNull(result.FileName);
            //Assert.NotNull(result.Description);
            //Assert.NotEqual(new DateTime(), result.Date);
            //Assert.NotNull(result.Version);
        }
    }
}