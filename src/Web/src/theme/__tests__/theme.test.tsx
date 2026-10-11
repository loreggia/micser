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
    ["Light", "light"],
    ["Dark", "dark"],
    ["HighContrast", "dark"],
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

  test("high contrast uses Fluent's high contrast theme", async () => {
    seed({ name: "HighContrast", colorMode: "Dark" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.theme).toBe(teamsHighContrastTheme);
  });

  test("shows a theme the UI doesn't have in the plain theme of its color mode", async () => {
    seed({ name: "Midnight", colorMode: "Dark" });

    const { result } = await renderHook(() => useTheme(), { wrapper });

    expect(result.current.preference?.name).toBe("Dark");
    expect(result.current.colorMode).toBe("dark");
  });
});
