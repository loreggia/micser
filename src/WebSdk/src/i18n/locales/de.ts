import type { Translations } from "../i18n";
import type { en } from "./en";

export const de: Translations<typeof en> = {
  information: "Informationen",
  about: "Über {{name}}",
  value: "Wert für {{name}}",
};
