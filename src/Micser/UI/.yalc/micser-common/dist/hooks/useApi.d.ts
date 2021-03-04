import { IApi, IProblem } from "../services";
export declare function useApi<R>(path: string): [IApi<R> | undefined, {
    isLoading: boolean;
    error?: IProblem;
}];
