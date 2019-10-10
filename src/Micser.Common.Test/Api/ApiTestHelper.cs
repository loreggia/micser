using Micser.Common.Api;
using Moq;
using System;

namespace Micser.Common.Test.Api
{
    public static class ApiTestHelper
    {
        public static ApiConfiguration GetConfiguration()
        {
            return new ApiConfiguration { PipeName = "Micser.Common.Test." + Guid.NewGuid() };
        }

        public static IRequestProcessorFactory GetRequestProcessorFactory()
        {
            var processorMock = new Mock<IRequestProcessor>();
            processorMock.Setup(p => p.ProcessAsync(It.IsAny<string>(), It.IsAny<object>())).ReturnsAsync((string n, object c) => new ApiResponse(true, c));
            var factoryMock = new Mock<IRequestProcessorFactory>();
            factoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(processorMock.Object);
            return factoryMock.Object;
        }
    }
}