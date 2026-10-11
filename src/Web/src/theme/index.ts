import {
  createDarkTheme,
  createLightTheme,
  teamsDarkTheme,
  teamsHighContrastTheme,
  teamsLightTheme,
  webDarkTheme,
  webLightTheme,
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
  /** Whether what doesn't use Fluent's tokens, e.g. React Flow's lines, takes the theme's colors and thicker lines. */
  highContrast?: boolean;
}

const defaultLight: ThemeDefinition = {
  name: "DefaultLight",
  colorMode: "Light",
  label: () => t("settings.themes.defaultLight"),
  theme: createLightTheme(brandVariants),
};
const defaultDark: ThemeDefinition = {
  name: "DefaultDark",
  colorMode: "Dark",
  label: () => t("settings.themes.defaultDark"),
  theme: createDarkTheme(brandVariants),
};

/**
 * Fluent's high contrast theme in light colors: its colors are roles (canvas, text, highlight, ...) of the dark contrast theme of Windows,
 * replaced by the same roles of the light one ("Desert").
 */
const lightContrastColors: Record<string, string> = {
  "#000000": "#ffffff", // canvas, highlighted and button text
  "#ffffff": "#000000", // text, button face
  "#1aebff": "#37006e", // highlight
  "#ffff00": "#00009f", // link
  "#3ff23f": "#600000", // disabled
};
const highContrastLightTheme = Object.fromEntries(
  Object.entries(teamsHighContrastTheme).map(([token, value]) => [
    token,
    typeof value === "string" ? (lightContrastColors[value.toLowerCase()] ?? value) : value,
  ])
) as Theme;

/** The themes the settings offer after "System", in their order. */
export const themes: ThemeDefinition[] = [
  defaultLight,
  defaultDark,
  // Fluent's web theme
  { name: "BlueLight", colorMode: "Light", label: () => t("settings.themes.blueLight"), theme: webLightTheme },
  { name: "BlueDark", colorMode: "Dark", label: () => t("settings.themes.blueDark"), theme: webDarkTheme },
  // Fluent's Teams theme
  { name: "PurpleLight", colorMode: "Light", label: () => t("settings.themes.purpleLight"), theme: teamsLightTheme },
  { name: "PurpleDark", colorMode: "Dark", label: () => t("settings.themes.purpleDark"), theme: teamsDarkTheme },
  {
    name: "HighContrastLight",
    colorMode: "Light",
    label: () => t("settings.themes.highContrastLight"),
    theme: highContrastLightTheme,
    highContrast: true,
  },
  {
    name: "HighContrastDark",
    colorMode: "Dark",
    label: () => t("settings.themes.highContrastDark"),
    theme: teamsHighContrastTheme,
    highContrast: true,
  },
];

export interface ResolvedTheme {
  /** The preferred theme; undefined follows the system. A preferred theme the UI doesn't have is the default one of its color mode. */
  preference: ThemeDefinition | undefined;
  theme: Theme;
  /** For what doesn't use Fluent's tokens (React Flow, `color-scheme`). */
  colorMode: "light" | "dark";
  /** See {@link ThemeDefinition.highContrast}. */
  highContrast: boolean;
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
      (preferred.colorMode === "Dark" ? defaultDark : defaultLight))
    : undefined;
  const { theme, colorMode, highContrast = false } = preference ?? (prefersDark ? defaultDark : defaultLight);

  return { preference, theme, colorMode: colorMode === "Dark" ? "dark" : "light", highContrast };
}
