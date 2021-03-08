import { Problem } from "../services";
export declare const useGetApi: <R, P = unknown>(path: string, action?: string, params?: P) => [R, {
    refresh: () => void;
    isLoading: boolean;
    error?: Problem;
}];
