import { Api, Problem } from "../services";
export declare function useApi<R>(path: string): [Api<R> | undefined, {
    isLoading: boolean;
    error?: Problem;
}];
