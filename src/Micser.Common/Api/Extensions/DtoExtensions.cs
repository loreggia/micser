using Micser.Common.Modules;

namespace Micser.Common.Api.Extensions
{
    public static class DtoExtensions
    {
        public static Module AsApiModel(this ModuleDto dto)
        {
            return new Module
            {
                Id = dto.Id,
                ModuleType = dto.ModuleType ?? "",
                WidgetType = dto.WidgetType ?? "",
                State = { dto.State }
            };
        }

        public static ModuleConnection AsApiModel(this ModuleConnectionDto dto)
        {
            return new ModuleConnection
            {
                Id = dto.Id,
                SourceConnectorName = dto.SourceConnectorName ?? "",
                SourceId = dto.SourceId,
                TargetConnectorName = dto.TargetConnectorName ?? "",
                TargetId = dto.TargetId
            };
        }

        public static ModuleDto AsDto(this Module module)
        {
            return new ModuleDto
            {
                Id = module.Id,
                ModuleType = module.ModuleType,
                WidgetType = module.WidgetType,
                State = { module.State }
            };
        }

        public static ModuleConnectionDto AsDto(this ModuleConnection connection)
        {
            return new ModuleConnectionDto
            {
                Id = connection.Id,
                SourceConnectorName = connection.SourceConnectorName,
                SourceId = connection.SourceId,
                TargetConnectorName = connection.TargetConnectorName,
                TargetId = connection.TargetId
            };
        }
    }
}