import { IProblem } from "../services";
export declare type ApiOptions = {
    onError?: (e: any) => void;
    autoLoad?: boolean;
};
export declare const useGetApi: <R, P = any>(path: string, action?: string, params?: P) => [R, {
    refresh: () => void;
    isLoading: boolean;
    error?: IProblem;
}];
