import {
  createDarkTheme,
  createLightTheme,
  teamsHighContrastTheme,
  type BrandVariants,
  type Theme,
} from "@fluentui/react-components";
import { usePreferences, type UiThemeDto } from "@micser/web-sdk";
import { useSyncExternalStore } from "react";
import { t } from "../i18n";

export const brandVariants: BrandVariants = {
  10: "#020402",
  20: "#131B10",
  30: "#1B2E19",
  40: "#213B1E",
  50: "#274924",
  60: "#2D5729",
  70: "#33662F",
  80: "#397535",
  90: "#3F853B",
  100: "#459440",
  110: "#5BA254",
  120: "#73AF6B",
  130: "#8ABC82",
  140: "#A0C999",
  150: "#B7D6B1",
  160: "#CDE3C9",
};

/** A theme the settings offer. */
export interface ThemeDefinition extends UiThemeDto {
  label: () => string;
  theme: Theme;
}

const lightTheme: ThemeDefinition = {
  name: "Light",
  colorMode: "Light",
  label: () => t("settings.themes.light"),
  theme: createLightTheme(brandVariants),
};
const darkTheme: ThemeDefinition = {
  name: "Dark",
  colorMode: "Dark",
  label: () => t("settings.themes.dark"),
  theme: createDarkTheme(brandVariants),
};

/** The themes the settings offer after "System", in their order. */
export const themes: ThemeDefinition[] = [
  lightTheme,
  darkTheme,
  {
    name: "HighContrast",
    colorMode: "Dark",
    label: () => t("settings.themes.highContrast"),
    theme: teamsHighContrastTheme,
  },
];

export interface ResolvedTheme {
  /** The preferred theme; undefined follows the system. A preferred theme the UI doesn't have is the light or dark one of its mode. */
  preference: ThemeDefinition | undefined;
  theme: Theme;
  /** For what doesn't use Fluent's tokens (React Flow, `color-scheme`). */
  colorMode: "light" | "dark";
}

const darkScheme = window.matchMedia("(prefers-color-scheme: dark)");

function usePrefersDark() {
  return useSyncExternalStore(
    (onChange) => {
      darkScheme.addEventListener("change", onChange);
      return () => darkScheme.removeEventListener("change", onChange);
    },
    () => darkScheme.matches
  );
}

/** The theme of the preferences; without one, the system's light or dark mode. */
export function useTheme(): ResolvedTheme {
  const [{ theme: preferred }] = usePreferences();
  const prefersDark = usePrefersDark();
  const preference = preferred
    ? (themes.find((theme) => theme.name === preferred.name) ??
      (preferred.colorMode === "Dark" ? darkTheme : lightTheme))
    : undefined;
  const { theme, colorMode } = preference ?? (prefersDark ? darkTheme : lightTheme);

  return { preference, theme, colorMode: colorMode === "Dark" ? "dark" : "light" };
}
