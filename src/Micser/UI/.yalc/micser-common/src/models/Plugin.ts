import { Widget } from "./Widget";

export interface Plugin {
    name: string;
    widgets?: Widget[];
    resources: {
        [key: string]: {};
    };
}
