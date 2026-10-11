import { teamsHighContrastTheme } from "@fluentui/react-components";
import { getGetPreferencesQueryKey, type UiThemeDto } from "@micser/web-sdk";
import { createTestQueryClient, TestProviders } from "@micser/web-sdk/testing";
import type { QueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, test } from "vitest";
import { renderHook } from "vitest-browser-react";
import { themes, useTheme } from "..";

let queryClient: QueryClient;

function wrapper({ children }: { children: ReactNode }) {
  return <TestProviders queryClient={queryClient}>{children}</TestProviders>;
}

function seed(theme: UiThemeDto | null) {
  queryClient.setQueryData(getGetPreferencesQueryKey(), {
    showStreamStatistics: false,
    snapToGrid: true,
    showChannelsByDefault: false,
    theme,
  });
}

beforeEach(() => {
  queryClient = createTestQueryClient();
});

describe("useTheme", () => {
  const systemColorMode = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";

  test.each([
    ["DefaultLight", "light"],
    ["DefaultDark", "dark"],
    ["BlueLight", "light"],
    ["BlueDark", "dark"],
    ["PurpleLight", "light"],
    ["PurpleDark", "dark"],
    ["HighContrastLight", "light"],
    ["HighContrastDark", "dark"],
  ])("%s has %s backgrounds", async (name, colorMode) => {
    seed(themes.find((theme) => theme.name === name)!);

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.preference?.name).toBe(name);
    expect(result.current.colorMode).toBe(colorMode);
  });

  test("follows the system without a theme", async () => {
    seed(null);

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.preference).toBeUndefined();
    expect(result.current.colorMode).toBe(systemColorMode);
  });

  test("dark high contrast uses Fluent's high contrast theme", async () => {
    seed({ name: "HighContrastDark", colorMode: "Dark" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.theme).toBe(teamsHighContrastTheme);
    expect(result.current.highContrast).toBe(true);
  });

  test("light high contrast has the light contrast colors in the same roles", async () => {
    seed({ name: "HighContrastLight", colorMode: "Light" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    const { theme } = result.current;
    expect(result.current.highContrast).toBe(true);
    expect([theme.colorNeutralBackground1, theme.colorNeutralForeground1]).toEqual(["#ffffff", "#000000"]);
    expect([theme.colorNeutralBackground1Hover, theme.colorNeutralForeground1Hover]).toEqual(["#37006e", "#ffffff"]);
    expect(theme.colorBrandForegroundLink).toBe("#00009f");
    expect(theme.colorNeutralForegroundDisabled).toBe("#600000");
    expect(theme.fontSizeBase300).toBe(teamsHighContrastTheme.fontSizeBase300);
  });

  test("other themes keep React Flow's colors", async () => {
    seed({ name: "DefaultDark", colorMode: "Dark" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.highContrast).toBe(false);
  });

  test("shows a theme the UI doesn't have in the default theme of its color mode", async () => {
    seed({ name: "Midnight", colorMode: "Dark" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.preference?.name).toBe("DefaultDark");
    expect(result.current.colorMode).toBe("dark");
  });
});
