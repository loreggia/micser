export interface Api<TData> {
    get: (action: string, param?: unknown) => Promise<ApiResultData<TData>>;
    getList: (action: string, param?: unknown) => Promise<ApiResultData<TData[]>>;
    post: (action: string, data: unknown) => Promise<ApiResultData<TData>>;
    put: (action: string | number, data: unknown) => Promise<ApiResultData<TData>>;
    delete: (action: string | number, data?: unknown) => Promise<ApiResultData<TData>>;
}
export declare type ErrorHandler = (error: Problem) => void;
export declare type BeginRequestHandler = () => void;
export declare type EndRequestHandler = (success: boolean) => void;
export interface Problem {
    type: string;
    status: number;
    title?: string;
    detail?: string;
    instance?: string;
    traceId?: string;
    errors?: unknown;
}
export interface ApiResult {
    isSuccess: boolean;
    problem?: Problem;
}
export interface ApiResultData<T> extends ApiResult {
    data?: T;
}
export declare class ApiService<TData> implements Api<TData> {
    private _axios;
    private _onError;
    private _onBeginRequest;
    private _onEndRequest;
    constructor(baseUrl: string, onBeginRequest: BeginRequestHandler, onEndRequest: EndRequestHandler, onError: ErrorHandler);
    get(action?: string, params?: unknown): Promise<ApiResultData<TData>>;
    getList(action?: string, params?: unknown): Promise<ApiResultData<TData[]>>;
    post(action: string, data: unknown): Promise<ApiResultData<TData>>;
    put(action: string | number, data: unknown): Promise<ApiResultData<TData>>;
    delete(action: string | number, params?: unknown): Promise<ApiResultData<TData>>;
    private execute;
    private getResult;
    private handleError;
}
