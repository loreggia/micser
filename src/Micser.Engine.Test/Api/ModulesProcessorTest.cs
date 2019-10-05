using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Common.Test;
using Micser.Engine.Api;
using Micser.Engine.Infrastructure.Services;
using Moq;
using NLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Micser.Engine.Test.Api
{
    public class ModulesProcessorTest
    {
        public ModulesProcessorTest(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelperTarget.ConfigureLogger(testOutputHelper);
        }

        [Fact]
        public async Task Delete()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            audioEngineMock.Setup(x => x.RemoveModule(It.IsAny<long>())).Verifiable();
            moduleServiceMock.Setup(x => x.Delete(It.IsAny<long>())).Returns<long>(id => new ModuleDto { Id = id }).Verifiable();

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var result = await modulesProcessor.ProcessAsync("delete", 4L).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            var content = result.Content as ModuleDto;
            Assert.NotNull(content);
            Assert.Equal(4, content.Id);
        }

        [Fact]
        public async Task GetAll()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            moduleServiceMock.Setup(x => x.GetAll()).Returns(new[] { new ModuleDto { Id = 42 } }).Verifiable();

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var result = await modulesProcessor.ProcessAsync("getall", null).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            var content = result.Content as IEnumerable<ModuleDto>;
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            Assert.Contains(content, m => m.Id == 42);
        }

        [Fact]
        public async Task InsertInvalidDto()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var dto = new ModuleDto();
            var result = await modulesProcessor.ProcessAsync("insert", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task InsertValidDto()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            audioEngineMock.Setup(x => x.AddModule(It.IsAny<long>())).Verifiable();
            moduleServiceMock.Setup(x => x.Insert(It.IsAny<ModuleDto>())).Returns(true).Verifiable();

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var dto = new ModuleDto
            {
                ModuleType = "ModuleType",
                WidgetType = "WidgetType",
                State = new ModuleState()
            };
            var result = await modulesProcessor.ProcessAsync("insert", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            audioEngineMock.Verify(x => x.AddModule(It.IsAny<long>()));
            moduleServiceMock.Verify(x => x.Insert(It.IsAny<ModuleDto>()));
        }

        [Fact]
        public async Task UpdateExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            moduleServiceMock.Setup(x => x.GetById(It.IsAny<long>())).Returns<long>(id => new ModuleDto { Id = id });
            moduleServiceMock.Setup(x => x.Update(It.IsAny<ModuleDto>())).Returns(true);
            audioEngineMock.Setup(x => x.UpdateModule(It.IsAny<long>())).Verifiable();

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var dto = new ModuleDto
            {
                Id = 3
            };
            var result = await modulesProcessor.ProcessAsync("update", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            var content = result.Content as ModuleDto;
            Assert.NotNull(content);
            Assert.Equal(3, content.Id);

            moduleServiceMock.Verify(x => x.Update(It.Is<ModuleDto>(m => m.Id == 3)));
            audioEngineMock.Verify(x => x.UpdateModule(It.Is<long>(id => id == 3)));
        }

        [Fact]
        public async Task UpdateNotExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var moduleServiceMock = new Mock<IModuleService>();
            var connectionServiceMock = new Mock<IModuleConnectionService>();

            moduleServiceMock.Setup(x => x.GetById(It.IsAny<long>())).Returns((ModuleDto)null);

            var modulesProcessor = new ModulesProcessor(audioEngineMock.Object, moduleServiceMock.Object, connectionServiceMock.Object, LogManager.GetCurrentClassLogger());

            var dto = new ModuleDto
            {
                Id = -1
            };
            var result = await modulesProcessor.ProcessAsync("update", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }
    }
}