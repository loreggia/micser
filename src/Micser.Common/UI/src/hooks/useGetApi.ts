import { useCallback, useEffect, useState } from "react";

import { Problem } from "../services";
import { useApi } from "./useApi";

export const useGetApi = <R, P = unknown>(
    path: string,
    action?: string,
    params?: P
): [R | undefined, { refresh: () => void; isLoading: boolean; error?: Problem }] => {
    const [api, { isLoading, error }] = useApi<R>(path);

    const [result, setResult] = useState<R>();
    const [refreshIndex, setRefreshIndex] = useState(0);

    useEffect(() => {
        let canceled = false;

        const loadData = async () => {
            if (api) {
                const result = await api.get(action || "", params);

                if (!canceled && result.isSuccess) {
                    setResult(result.data || null);
                }
            }
        };

        loadData();

        return () => {
            canceled = true;
        };
    }, [api, action, params, refreshIndex]);

    const refresh = useCallback(() => {
        setRefreshIndex((i) => i + 1);
    }, []);

    return [result, { refresh, isLoading, error }];
};
