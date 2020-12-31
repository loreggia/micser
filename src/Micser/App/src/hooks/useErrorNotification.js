import { useEffect } from "react";
import { showError } from "../utils";

const useErrorNotification = (errors) => {
    useEffect(() => {
        errors.forEach(showError);
    }, errors);
};

export default useErrorNotification;
