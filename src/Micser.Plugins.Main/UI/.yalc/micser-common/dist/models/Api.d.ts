/// <reference types="react" />
export interface DTO {
    id: number;
}
export interface Module extends DTO {
    type: string;
    state: {
        title?: string;
        left: string;
        top: string;
        [key: string]: string;
    };
}
export interface ModuleConnection extends DTO {
    sourceId: number;
    targetId: number;
    sourceConnectorName: string;
    targetConnectorName: string;
}
export interface ModuleDescription {
    name: string;
    title: React.ReactNode;
    description: React.ReactNode;
}
export interface PluginDefinition {
    assemblyName: string;
    moduleName: string;
}
