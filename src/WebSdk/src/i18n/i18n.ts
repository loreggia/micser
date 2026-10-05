import i18next from "i18next";
import { initReactI18next, useTranslation as useI18nextTranslation } from "react-i18next";

/**
 * The UI's languages, with their names in their own language. The first one is the default and fallback.
 */
export const languages = [
  { code: "en", name: "English" },
  { code: "de", name: "Deutsch" },
] as const;

export type Language = (typeof languages)[number]["code"];

const codes: readonly string[] = languages.map((language) => language.code);

function isLanguage(code: string | null | undefined): code is Language {
  return !!code && codes.includes(code);
}

/**
 * The language to show: the preference if the UI has it, otherwise the first of the browser's languages it has (in WebView2 the OS
 * language), otherwise English. Regional variants count as their language, e.g. "de-AT" as "de".
 */
export function resolveLanguage(preference?: string | null): Language {
  if (isLanguage(preference)) {
    return preference;
  }

  const browserLanguages = typeof navigator === "undefined" ? [] : (navigator.languages ?? [navigator.language]);
  for (const browserLanguage of browserLanguages) {
    const code = browserLanguage.split("-")[0].toLowerCase();
    if (isLanguage(code)) {
      return code;
    }
  }

  return languages[0].code;
}

/**
 * The i18next instance shared by the UI and the plugins' widgets (through the shared `@micser/web-sdk` module).
 */
export const i18n = i18next.createInstance();

i18n.on("languageChanged", (language) => {
  if (typeof document !== "undefined") {
    document.documentElement.lang = language;
  }
});

void i18n.use(initReactI18next).init({
  lng: resolveLanguage(),
  fallbackLng: languages[0].code,
  supportedLngs: codes,
  resources: {},
  initAsync: false,
  interpolation: { escapeValue: false },
  react: { useSuspense: false },
});

/**
 * The current language.
 */
export function currentLanguage(): Language {
  return isLanguage(i18n.language) ? i18n.language : languages[0].code;
}

/**
 * Returns the current language and re-renders the component when it changes.
 */
export function useLanguage(): Language {
  useI18nextTranslation();
  return currentLanguage();
}

/**
 * Translations with the same keys as T, e.g. the German ones for the English T.
 */
export type Translations<T> = { [K in keyof T]: T[K] extends string ? string : Translations<T[K]> };

type PluralSuffix = "zero" | "one" | "two" | "few" | "many" | "other";

type WithoutPluralSuffix<K extends string> = K extends `${infer Key}_${PluralSuffix}` ? Key : K;

/**
 * The keys of a translation tree: paths joined with dots, plural forms (`key_one`, `key_other`) as their key.
 */
export type TranslationKey<T, Prefix extends string = ""> = {
  [K in keyof T & string]: T[K] extends string
    ? `${Prefix}${WithoutPluralSuffix<K>}`
    : TranslationKey<T[K], `${Prefix}${K}.`>;
}[keyof T & string];

/**
 * Values for the `{{name}}` placeholders; `count` also picks the plural form.
 */
export type TranslationValues = Record<string, unknown> & { count?: number };

export type Translate<T> = (key: TranslationKey<T>, values?: TranslationValues) => string;

/**
 * Text shown in the UI: fixed, or a function that translates it into the current language.
 */
export type LocalizedText = string | (() => string);

export function localize(text: LocalizedText): string {
  return typeof text === "function" ? text() : text;
}

/**
 * Adds translations under a namespace, e.g. a plugin's name, and returns functions that translate its keys. The keys come from the English
 * translations; the others need the same keys, and missing ones fall back to English.
 *
 * @example
 * export const { t, useTranslation } = defineTranslations("main", { en, de });
 */
export function defineTranslations<T extends object>(
  namespace: string,
  resources: { en: T } & Record<Language, Translations<T>>
) {
  for (const [language, translations] of Object.entries(resources)) {
    i18n.addResourceBundle(language, namespace, translations, true, true);
  }

  const t = i18n.getFixedT(null, namespace) as unknown as Translate<T>;

  return {
    /**
     * Translates into the current language. Components use {@link useTranslation} instead, so they re-render when the language changes.
     */
    t: ((key, values) => t(key, values)) as Translate<T>,

    /**
     * Returns the translate function and the current language, and re-renders the component when the language changes.
     */
    useTranslation() {
      const { t: translate } = useI18nextTranslation(namespace);
      return { t: translate as unknown as Translate<T>, language: currentLanguage() };
    },
  };
}
