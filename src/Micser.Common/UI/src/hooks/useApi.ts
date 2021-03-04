import { useEffect, useState } from "react";
import { Api, IApi, IProblem } from "services";

export function useApi<R>(path: string): [IApi<R>, { isLoading: boolean; error?: IProblem }] {
    const [api, setApi] = useState<IApi<R>>();
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
