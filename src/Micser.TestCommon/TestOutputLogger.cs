using NLog;
using NLog.Config;
using Xunit.Abstractions;

namespace Micser.TestCommon
{
    public static class TestOutputLogger
    {
        public static void Configure(ITestOutputHelper testOutputHelper)
        {
            var config = new LoggingConfiguration();
            config.AddTarget("Test", new TestOutputHelperTarget(testOutputHelper));
            config.AddRuleForAllLevels("Test");
            LogManager.Configuration = config;
        }
    }
}