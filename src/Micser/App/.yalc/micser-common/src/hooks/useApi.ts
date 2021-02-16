import { useEffect, useState } from "react";
import { Api, IProblem } from "../services";

export function useApi<R>(path: string): [Nullable<Api<R>>, { isLoading: boolean; error?: IProblem }] {
    const [api, setApi] = useState<Nullable<Api<R>>>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<IProblem>();

    useEffect(() => {
        let canceled = false;
        const onError = (error: IProblem) => {
            if (!canceled) {
                setError(error);
            }
        };

        const onRequestAction = (isLoading: boolean) => {
            if (!canceled) {
                setIsLoading(isLoading);
            }
        };

        const api = new Api<R>(
            path,
            () => onRequestAction(true),
            () => onRequestAction(false),
            onError
        );

        setApi(api);

        return () => {
            canceled = true;
        };
    }, []);

    return [api, { isLoading, error }];
}
