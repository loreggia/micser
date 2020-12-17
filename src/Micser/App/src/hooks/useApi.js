import { useCallback, useMemo, useState } from "react";
import AxiosStatic from "axios";
import trimStart from "lodash/trimStart";
import trimEnd from "lodash/trimEnd";

const useApi = (baseUrl, { defaultMethod, onError } = { defaultMethod: "get" }) => {
    const axios = useMemo(
        () =>
            AxiosStatic.create({
                baseURL: "/api",
            }),
        []
    );

    const [isLoading, setIsLoading] = useState(false);

    const resolveMethod = useCallback((method) => (method || defaultMethod || "get").toLowerCase(), [defaultMethod]);
    const resolveUrl = useCallback((url) => trimEnd(baseUrl, "/") + "/" + trimStart(url, "/"), [baseUrl]);

    const action = useCallback(
        async (action, data, method) => {
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
            } catch (error) {
                console.log(error);
                onError &&
                    onError(
                        error.response && error.response.data
                            ? error.response.data
                            : { statusCode: 500, message: "Unknown error." }
                    );
            } finally {
                setIsLoading(false);
            }
            return result;
        },
        [resolveUrl, resolveMethod, axios, onError]
    );

    return [action, isLoading];
};

export default useApi;
