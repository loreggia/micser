import { defineTranslations } from "@micser/web-sdk";
import { de } from "./locales/de";
import { en } from "./locales/en";

export const { t, useTranslation } = defineTranslations("main", { en, de });
