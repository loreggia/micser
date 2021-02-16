import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import defaultEn from "./en/default.json";

import { Languages } from "~/constants";

export const resources = {
    en: {
        default: defaultEn,
    },
} as const;

// .use(Backend)
i18n.use(initReactI18next)
    // init i18next
    // for all options read: https://www.i18next.com/overview/configuration-options
    .init({
        resources,
        fallbackLng: "en",
        fallbackNS: "default",
        ns: ["default"],
        debug: process.env.NODE_ENV !== "production",
        supportedLngs: Languages,
        interpolation: {
            escapeValue: false, // not needed for react as it escapes by default
        },
    });

export default i18n;
