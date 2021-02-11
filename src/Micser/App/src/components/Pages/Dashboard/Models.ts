export interface DTO {
    id: number;
}

export interface Module extends DTO {
    state: {
        left: string;
        top: string;
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
