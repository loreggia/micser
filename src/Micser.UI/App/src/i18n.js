import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import Backend from "i18next-http-backend";

import { Languages } from "./utils/constants";

i18n.use(Backend)
    .use(initReactI18next)
    // init i18next
    // for all options read: https://www.i18next.com/overview/configuration-options
    .init({
        lng: "en",
        ns: "default",
        defaultNS: "default",
        fallbackNS: "default",
        fallbackLng: false,
        debug: process.env.NODE_ENV !== "production",
        supportedLngs: Languages,
        interpolation: {
            escapeValue: false, // not needed for react as it escapes by default
        },
    });

export default i18n;
