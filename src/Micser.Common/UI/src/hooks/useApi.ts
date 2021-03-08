import { useContext, useEffect, useState } from "react";
import { Contexts } from "../Contexts";

import { Api, ApiService, Problem } from "../services";

export function useApi<R>(path: string): [Api<R> | undefined, { isLoading: boolean; error?: Problem }] {
    const [api, setApi] = useState<Api<R>>();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<Problem>();
    const errorContext = useContext(Contexts.error);

    useEffect(() => {
        let canceled = false;
        const onError = (error: Problem) => {
            if (!canceled) {
                setError(error);
            }
            errorContext.onError(error);
        };

        const onBeginRequest = () => {
            if (!canceled) {
                setIsLoading(true);
            }
        };

        const onEndRequest = (success: boolean) => {
            if (!canceled) {
                setIsLoading(false);
                if (success) {
                    setError(undefined);
                }
            }
        };

        const api = new ApiService<R>(path, onBeginRequest, onEndRequest, onError);

        setApi(api);

        return () => {
            canceled = true;
        };
    }, [path, errorContext]);

    return [api, { isLoading, error }];
}
