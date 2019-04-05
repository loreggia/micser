using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit.Abstractions;

namespace Micser.Common.Test
{
    [Target("Test")]
    public class TestOutputHelperTarget : TargetWithLayout
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestOutputHelperTarget(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static void ConfigureLogger(ITestOutputHelper testOutputHelper)
        {
            var config = new LoggingConfiguration();
            config.AddTarget("Test", new TestOutputHelperTarget(testOutputHelper));
            config.AddRuleForAllLevels("Test");
            LogManager.Configuration = config;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var message = Layout.Render(logEvent);
            _testOutputHelper.WriteLine(message);
        }
    }
}