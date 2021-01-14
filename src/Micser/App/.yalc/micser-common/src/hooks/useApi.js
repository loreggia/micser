import { useCallback, useEffect, useMemo, useState } from "react";
import AxiosStatic from "axios";
import trimStart from "lodash/trimStart";
import trimEnd from "lodash/trimEnd";

const useApi = (baseUrl, { defaultMethod, onError, autoLoad } = { defaultMethod: "get", autoLoad: true }) => {
    const axios = useMemo(
        () =>
            AxiosStatic.create({
                baseURL: "/api",
                withCredentials: true,
            }),
        []
    );

    const [result, setResult] = useState();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState();

    const resolveMethod = useCallback((method) => (method || defaultMethod || "get").toLowerCase(), [defaultMethod]);
    const resolveUrl = useCallback((url) => (url ? trimEnd(baseUrl, "/") + "/" + trimStart(url, "/") : baseUrl), [
        baseUrl,
    ]);

    const handleError = useCallback(
        (error) => {
            setError(error);
            if (onError) {
                onError(error);
            }
        },
        [onError]
    );

    const action = useCallback(
        async ({ data, action, method, setResult: enableSetResult } = {}) => {
            let result = null;
            setIsLoading(true);
            try {
                const url = resolveUrl(action);
                switch (resolveMethod(method)) {
                    case "get":
                        result = await axios.get(url, {
                            params: data,
                        });
                        break;
                    case "post":
                        result = await axios.post(url, data);
                        break;
                    case "put":
                        result = await axios.put(url, data);
                        break;
                    case "delete":
                        result = await axios.delete(url, {
                            params: data,
                        });
                        break;
                    default:
                        throw new Error("Invalid method");
                }
                if (enableSetResult) {
                    setResult(result.data);
                }
            } catch (error) {
                console.log(error);
                handleError(
                    error.response && error.response.data
                        ? error.response.data
                        : { statusCode: 500, message: "Unknown error." }
                );
            } finally {
                setIsLoading(false);
            }
            return result;
        },
        [resolveUrl, resolveMethod, axios, handleError]
    );

    useEffect(() => {
        if (autoLoad) {
            action({ setResult: true });
        }
    }, [action, autoLoad]);

    return { result, load: action, isLoading, error };
};

export default useApi;
