using Micser.Common.Audio;
using Micser.Common.Modules;
using Micser.Engine.Api;
using Micser.Engine.Infrastructure.Services;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Micser.Engine.Test.Api
{
    public class ModuleConnectionsProcessorTest
    {
        [Fact]
        public async Task DeleteExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            serviceMock.Setup(x => x.Delete(It.IsAny<long>())).Returns<long>(id => new ModuleConnectionDto { Id = id }).Verifiable();
            audioEngineMock.Setup(x => x.RemoveConnection(It.IsAny<long>())).Verifiable();

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var result = await processor.ProcessAsync("delete", 1L).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            serviceMock.Verify(x => x.Delete(It.Is<long>(id => id == 1L)));
            audioEngineMock.Verify(x => x.RemoveConnection(It.Is<long>(id => id == 1L)));
        }

        [Fact]
        public async Task DeleteNotExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var result = await processor.ProcessAsync("delete", -1L).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GetAll()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            serviceMock.Setup(x => x.GetAll()).Returns(new[] { new ModuleConnectionDto { Id = 69 } });

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var result = await processor.ProcessAsync("getall", null).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            var content = result.Content as IEnumerable<ModuleConnectionDto>;
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            Assert.Contains(content, m => m.Id == 69);
        }

        [Fact]
        public async Task InsertInvalidDto()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var dto = new ModuleConnectionDto();
            var result = await processor.ProcessAsync("insert", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task InsertValidDto()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            audioEngineMock.Setup(x => x.AddConnection(It.IsAny<long>())).Verifiable();
            serviceMock.Setup(x => x.Insert(It.IsAny<ModuleConnectionDto>())).Returns(true);

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var dto = new ModuleConnectionDto
            {
                SourceId = 1,
                SourceConnectorName = "Source",
                TargetId = 2,
                TargetConnectorName = "Target"
            };
            var result = await processor.ProcessAsync("insert", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            audioEngineMock.Verify(x => x.AddConnection(It.IsAny<long>()));
        }

        [Fact]
        public async Task UpdateExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            audioEngineMock.Setup(x => x.RemoveConnection(It.IsAny<long>())).Verifiable();
            audioEngineMock.Setup(x => x.AddConnection(It.IsAny<long>())).Verifiable();
            serviceMock.Setup(x => x.GetById(It.IsAny<long>())).Returns<long>(id => new ModuleConnectionDto { Id = id });
            serviceMock.Setup(x => x.Update(It.IsAny<ModuleConnectionDto>())).Returns(true);

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var dto = new ModuleConnectionDto
            {
                Id = 7,
                SourceId = 11,
                SourceConnectorName = "Source!",
                TargetId = 22,
                TargetConnectorName = "Target!"
            };
            var result = await processor.ProcessAsync("update", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);

            audioEngineMock.Verify(x => x.RemoveConnection(It.Is<long>(id => id == 7)));
            audioEngineMock.Verify(x => x.AddConnection(It.Is<long>(id => id == 7)));
        }

        [Fact]
        public async Task UpdateNotExisting()
        {
            var audioEngineMock = new Mock<IAudioEngine>();
            var serviceMock = new Mock<IModuleConnectionService>();

            var processor = new ModuleConnectionsProcessor(audioEngineMock.Object, serviceMock.Object);

            var dto = new ModuleConnectionDto { Id = -1 };
            var result = await processor.ProcessAsync("update", dto).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
        }
    }
}