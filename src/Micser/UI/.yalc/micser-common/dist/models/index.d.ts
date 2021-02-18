import { Widget } from "./Widget";
export * from "./Api";
export * from "./Widget";
export interface Plugin {
    name: string;
    widgets?: Widget[];
    resources: {
        [key: string]: {};
    };
}
