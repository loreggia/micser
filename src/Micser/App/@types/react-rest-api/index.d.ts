declare module "react-rest-api" {
    import { FC } from "react";

    export interface HookResult<TResult> {
        result: TResult;
        loading: boolean;
        error: any;
    }

    export interface Api<TData> {
        get: (endpoint: string, conf?: {}, params?: any) => Promise<TData>;
        post: (endpoint: string, conf?: {}, params?: any) => Promise<TData>;
        put: (endpoint: string, conf?: {}, params?: any) => Promise<TData>;
        del: (endpoint: string, conf?: {}, params?: any) => Promise<TData>;
    }

    export function useGet<TResult>(url: string): HookResult<TResult>;
    export function usePost<TResult>(url: string): HookResult<TResult>;
    export function usePut<TResult>(url: string): HookResult<TResult>;
    export function useDelete<TResult>(url: string): HookResult<TResult>;
    export function useApi<TData>(): Api<TData>;

    export interface ApiProviderProps {
        url: string;
        config: {};
        resolveCallback: (response: Response) => Promise;
        rejectCallback: (response: Response) => Promise;
    }
    export declare const ApiProvider: FC<ApiProviderProps>;
}
