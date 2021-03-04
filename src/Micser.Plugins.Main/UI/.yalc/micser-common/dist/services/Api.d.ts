export interface ApiOptions {
    onError?: (e: unknown) => void;
    autoLoad?: boolean;
}
export interface IApi<TData> {
    get: (action: string, param?: unknown) => Promise<IApiResultData<TData>>;
    getList: (action: string, param?: unknown) => Promise<IApiResultData<TData[]>>;
    post: (action: string, data: unknown) => Promise<IApiResultData<TData>>;
    put: (action: string | number, data: unknown) => Promise<IApiResultData<TData>>;
    delete: (action: string | number, data?: unknown) => Promise<IApiResultData<TData>>;
}
export declare type ErrorHandler = (error: IProblem) => void;
export declare type RequestAction = () => void;
export interface IProblem {
    type: string;
    status: number;
    title?: string;
    detail?: string;
    instance?: string;
    traceId?: string;
    errors?: unknown;
}
export interface IApiResult {
    isSuccess: boolean;
    problem?: IProblem;
}
export interface IApiResultData<T> extends IApiResult {
    data?: T;
}
export declare class Api<TData> implements IApi<TData> {
    private _axios;
    private _onError;
    private _onBeginRequest;
    private _onEndRequest;
    constructor(baseUrl: string, onBeginRequest: RequestAction, onEndRequest: RequestAction, onError: ErrorHandler);
    get(action?: string, params?: unknown): Promise<IApiResultData<TData>>;
    getList(action?: string, params?: unknown): Promise<IApiResultData<TData[]>>;
    post(action: string, data: unknown): Promise<IApiResultData<TData>>;
    put(action: string | number, data: unknown): Promise<IApiResultData<TData>>;
    delete(action: string | number, params?: unknown): Promise<IApiResultData<TData>>;
    private execute;
    private getResult;
    private handleError;
}
