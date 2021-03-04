import AxiosStatic, { AxiosError, AxiosInstance, AxiosResponse } from "axios";
import { trimStart } from "lodash";

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

export type ErrorHandler = (error: IProblem) => void;

export type RequestAction = () => void;

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

export class Api<TData> implements IApi<TData> {
    private _axios: AxiosInstance;
    private _onError: ErrorHandler;
    private _onBeginRequest: RequestAction;
    private _onEndRequest: RequestAction;

    constructor(baseUrl: string, onBeginRequest: RequestAction, onEndRequest: RequestAction, onError: ErrorHandler) {
        this._axios = AxiosStatic.create({
            withCredentials: true,
            baseURL: "/api/" + trimStart(baseUrl, "/"),
        });
        this._onBeginRequest = onBeginRequest;
        this._onEndRequest = onEndRequest;
        this._onError = onError;
    }

    async get(action = "", params?: unknown): Promise<IApiResultData<TData>> {
        return this.execute(() => this._axios.get<TData>(action, { params }));
    }

    async getList(action = "", params?: unknown): Promise<IApiResultData<TData[]>> {
        return this.execute<TData[]>(() => this._axios.get<TData[]>(action, { params }));
    }

    async post(action = "", data: unknown): Promise<IApiResultData<TData>> {
        return this.execute(() => this._axios.post<TData>(action, data));
    }

    async put(action: string | number, data: unknown): Promise<IApiResultData<TData>> {
        return this.execute(() => this._axios.put<TData>(`${action}`, data));
    }

    async delete(action: string | number, params?: unknown): Promise<IApiResultData<TData>> {
        return this.execute(() => this._axios.delete<TData>(`${action}`, { params }));
    }

    private async execute<TData>(func: () => Promise<AxiosResponse<TData>>): Promise<IApiResultData<TData>> {
        try {
            this._onBeginRequest();
            const result = await func();
            return this.getResult<TData>(result);
        } catch (error) {
            return this.handleError<TData>(error);
        } finally {
            this._onEndRequest();
        }
    }

    private getResult<TData>(axiosResult: AxiosResponse<TData>): IApiResultData<TData> {
        const isSuccess = axiosResult.status >= 200 && axiosResult.status < 400;
        return {
            isSuccess,
            problem: isSuccess ? undefined : ((axiosResult.data as unknown) as IProblem),
            data: isSuccess ? axiosResult.data : undefined,
        };
    }

    private handleError<TData>(error: AxiosError): IApiResultData<TData> {
        // console.log(error);
        const problem: IProblem =
            error.response && error.response.data
                ? (error.response.data as IProblem)
                : {
                      type: "Internal",
                      status: 500,
                      title: "Unknown error.",
                  };
        this._onError(problem);
        return {
            isSuccess: false,
            problem,
        };
    }
}
