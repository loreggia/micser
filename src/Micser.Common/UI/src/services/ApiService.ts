import AxiosStatic, { AxiosError, AxiosInstance, AxiosResponse } from "axios";
import { trimStart } from "lodash";

export interface Api<TData> {
    get: (action: string, param?: unknown) => Promise<ApiResultData<TData>>;
    getList: (action: string, param?: unknown) => Promise<ApiResultData<TData[]>>;
    post: (action: string, data: unknown) => Promise<ApiResultData<TData>>;
    put: (action: string | number, data: unknown) => Promise<ApiResultData<TData>>;
    delete: (action: string | number, data?: unknown) => Promise<ApiResultData<TData>>;
}

export type ErrorHandler = (error: Problem) => void;

export type BeginRequestHandler = () => void;
export type EndRequestHandler = (success: boolean) => void;

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

export class ApiService<TData> implements Api<TData> {
    private _axios: AxiosInstance;
    private _onError: ErrorHandler;
    private _onBeginRequest: BeginRequestHandler;
    private _onEndRequest: EndRequestHandler;

    constructor(
        baseUrl: string,
        onBeginRequest: BeginRequestHandler,
        onEndRequest: EndRequestHandler,
        onError: ErrorHandler
    ) {
        this._axios = AxiosStatic.create({
            withCredentials: true,
            baseURL: "/api/" + trimStart(baseUrl, "/"),
        });
        this._onBeginRequest = onBeginRequest;
        this._onEndRequest = onEndRequest;
        this._onError = onError;
    }

    async get(action = "", params?: unknown): Promise<ApiResultData<TData>> {
        return this.execute(() => this._axios.get<TData>(action, { params }));
    }

    async getList(action = "", params?: unknown): Promise<ApiResultData<TData[]>> {
        return this.execute<TData[]>(() => this._axios.get<TData[]>(action, { params }));
    }

    async post(action = "", data: unknown): Promise<ApiResultData<TData>> {
        return this.execute(() => this._axios.post<TData>(action, data));
    }

    async put(action: string | number, data: unknown): Promise<ApiResultData<TData>> {
        return this.execute(() => this._axios.put<TData>(`${action}`, data));
    }

    async delete(action: string | number, params?: unknown): Promise<ApiResultData<TData>> {
        return this.execute(() => this._axios.delete<TData>(`${action}`, { params }));
    }

    private async execute<TData>(func: () => Promise<AxiosResponse<TData>>): Promise<ApiResultData<TData>> {
        let success = true;

        try {
            this._onBeginRequest();
            const result = await func();
            return this.getResult<TData>(result);
        } catch (error) {
            success = false;
            return this.handleError<TData>(error);
        } finally {
            this._onEndRequest(success);
        }
    }

    private getResult<TData>(axiosResult: AxiosResponse<TData>): ApiResultData<TData> {
        const isSuccess = axiosResult.status >= 200 && axiosResult.status < 400;
        return {
            isSuccess,
            problem: isSuccess ? undefined : ((axiosResult.data as unknown) as Problem),
            data: isSuccess ? axiosResult.data : undefined,
        };
    }

    private handleError<TData>(error: AxiosError): ApiResultData<TData> {
        // eslint-disable-next-line no-console
        const problem: Problem =
            error.response && error.response.data
                ? (error.response.data as Problem)
                : {
                      type: "Internal",
                      status: error.code ? Number(error.code) : 500,
                      title: error.message || "Unknown error.",
                  };
        this._onError(problem);
        return {
            isSuccess: false,
            problem,
        };
    }
}
