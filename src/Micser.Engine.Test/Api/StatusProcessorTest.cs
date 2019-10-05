using Micser.Engine.Api;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Engine.Test.Api
{
    public class StatusProcessorTest
    {
        [Fact]
        public async Task GetStatus()
        {
            var statusProcessor = new StatusProcessor();

            var result = await statusProcessor.ProcessAsync(null, null).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }
    }
}