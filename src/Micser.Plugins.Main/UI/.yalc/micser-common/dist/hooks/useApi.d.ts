import { IApi, IProblem } from "services";
export declare function useApi<R>(path: string): [IApi<R>, {
    isLoading: boolean;
    error?: IProblem;
}];
