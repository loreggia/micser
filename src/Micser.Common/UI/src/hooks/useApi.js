import { useEffect, useMemo, useState } from "react";
import AxiosStatic from "axios";

const useApi = (url, { autoLoad, method, data } = { autoLoad: true, method: "get" }) => {
    const axios = useMemo(
        () =>
            AxiosStatic.create({
                baseURL: "/api",
            }),
        []
    );

    const [result, setResult] = useState();
    const [isLoading, setIsLoading] = useState(false);
    const [isLoaded, setIsLoaded] = useState(false);
    const [error, setError] = useState();
    const [refreshIndex, setRefreshIndex] = useState(0);

    const refresh = () => {
        setRefreshIndex(refreshIndex + 1);
    };

    useEffect(() => {
        let isCancelled = false;

        const loadAsync = async () => {
            setIsLoading(true);
            setIsLoaded(false);

            try {
                let result = null;
                switch (method) {
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
                if (!isCancelled) {
                    setResult(result.data);
                    setError(null);
                    setIsLoading(false);
                    setIsLoaded(true);
                }
            } catch (error) {
                console.log(error);
                if (!isCancelled) {
                    setError(
                        error.response && error.response.data
                            ? error.response.data
                            : { statusCode: 500, message: "Unknown error." }
                    );
                }
            } finally {
                if (!isCancelled) {
                    setIsLoading(false);
                }
            }
        };

        if (autoLoad || refreshIndex > 1) {
            loadAsync();
        }

        return () => {
            isCancelled = true;
        };
    }, [axios, url, autoLoad, method, data, refreshIndex]);

    return [result, isLoading, refresh, isLoaded, error];
};

export default useApi;
