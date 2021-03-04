export const parseBool = (value?: string): boolean => {
    return value && value.toLowerCase() === "true";
};
