import { defineTranslations } from "./i18n";
import { de } from "./locales/de";
import { en } from "./locales/en";

/** The SDK's own texts, e.g. the accessible names of its controls. */
export const { useTranslation } = defineTranslations("sdk", { en, de });
