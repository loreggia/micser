import { IProblem } from "../services";
export declare type ApiOptions = {
    onError?: (e: unknown) => void;
    autoLoad?: boolean;
};
export declare const useGetApi: <R, P = unknown>(path: string, action?: string, params?: P) => [R, {
    refresh: () => void;
    isLoading: boolean;
    error?: IProblem;
}];
