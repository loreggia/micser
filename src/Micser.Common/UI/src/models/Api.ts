export interface DTO {
    id: number;
}

export interface Module extends DTO {
    type: string;
    state: ModuleState;
}

export interface ModuleState {
    title?: string;
    left: string;
    top: string;
    [key: string]: string | undefined;
}

export interface ModuleConnection extends DTO {
    sourceId: number;
    targetId: number;
    sourceConnectorName: string;
    targetConnectorName: string;
}

export interface ModuleDefinition {
    name: string;
}

export interface PluginDefinition {
    assemblyName: string;
    moduleName: string;
}

export interface DeviceDescription {
    id: string;
    friendlyName: string;
}
