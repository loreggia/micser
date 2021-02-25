import { Api, IProblem } from "services";
export declare function useApi<R>(path: string): [Nullable<Api<R>>, {
    isLoading: boolean;
    error?: IProblem;
}];
