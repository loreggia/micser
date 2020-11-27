using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Micser.TestCommon
{
    public class TestLogger<TCategory> : ILogger<TCategory>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestLogger(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _testOutputHelper.WriteLine($"{logLevel}|{formatter(state, exception)}");
        }

        private class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}