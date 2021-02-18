import { FC } from "react";
export interface LoaderProps {
    isVisible?: boolean;
    tip?: string;
    suspenseTime?: number;
}
export declare const Loader: FC<LoaderProps>;
