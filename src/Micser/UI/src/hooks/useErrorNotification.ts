import { useEffect } from "react";
import { ErrorType, showError } from "~/utils";

export const useErrorNotification = (errors: ErrorType[]): void => {
    useEffect(() => {
        errors.forEach(showError);
        // eslint-disable-next-line
    }, errors);
};
