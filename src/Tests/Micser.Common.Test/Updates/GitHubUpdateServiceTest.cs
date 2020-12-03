using System.Threading.Tasks;
using Micser.Common.Updates;
using Micser.TestCommon;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Common.Test.Updates
{
    public class GitHubUpdateServiceTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GitHubUpdateServiceTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task GetUpdateManifest()
        {
            var service = new GitHubUpdateService(new TestLogger<GitHubUpdateService>(_testOutputHelper));

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