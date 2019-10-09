using NLog;
using NLog.Targets;
using Xunit.Abstractions;

namespace Micser.TestCommon
{
    [Target("Test")]
    public class TestOutputHelperTarget : TargetWithLayout
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputHelperTarget(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            _testOutputHelper.WriteLine(message);
        }
    }
}