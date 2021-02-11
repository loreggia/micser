export type HookResult<TResult> = {
    result: TResult;
    loading: boolean;
    error: any;
};

export function useGet<TResult>(url: string): HookResult<TResult>;
export function usePost<TResult>(url: string): HookResult<TResult>;
export function usePut<TResult>(url: string): HookResult<TResult>;
export function useDelete<TResult>(url: string): HookResult<TResult>;
