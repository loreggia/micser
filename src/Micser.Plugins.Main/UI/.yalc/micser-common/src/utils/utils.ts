export const parseBool = (value?: string) => {
    return value && value.toLowerCase() === "true";
};
