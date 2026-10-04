import type { QueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { renderHook } from "vitest-browser-react";
import { createTestQueryClient, testModule, TestProviders } from "../../testing";
import { getGetPreferencesQueryKey, updatePreferences, type UiPreferencesDto } from "../api";
import { EngineConnection, type ModuleLevels } from "./EngineConnection";
import {
  useEngineConnection,
  useEngineConnectionState,
  useModuleData,
  useModuleLevels,
  useModuleUpdate,
  usePreferences,
} from "./hooks";

vi.mock("../api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api")>()),
  updatePreferences: vi.fn(),
}));

const savePreferences = vi.mocked(updatePreferences);
let queryClient: QueryClient;
let connection: EngineConnection;

function wrapper({ children }: { children: ReactNode }) {
  return (
    <TestProviders queryClient={queryClient} connection={connection}>
      {children}
    </TestProviders>
  );
}

beforeEach(() => {
  queryClient = createTestQueryClient();
  connection = new EngineConnection(queryClient);
});

afterEach(() => {
  savePreferences.mockReset();
  vi.unstubAllGlobals();
});

test("useEngineConnection needs an EngineProvider", async () => {
  vi.spyOn(console, "error").mockImplementation(() => {});

  await expect(renderHook(() => useEngineConnection())).rejects.toThrow("inside an EngineProvider");
});

test("useEngineConnectionState follows the connection", async () => {
  let notify: (() => void) | undefined;
  vi.spyOn(connection, "onStateChanged").mockImplementation((listener) => {
    notify = () => listener("connected");
    return () => {};
  });
  const state = vi.spyOn(connection, "state", "get").mockReturnValue("connecting");

  const { result, act } = await renderHook(() => useEngineConnectionState(), { wrapper });
  expect(result.current).toBe("connecting");

  state.mockReturnValue("connected");
  await act(() => notify!());
  expect(result.current).toBe("connected");
});

describe("usePreferences", () => {
  const stored: UiPreferencesDto = { showStreamStatistics: false, snapToGrid: true };

  test("has the defaults until the preferences are loaded", async () => {
    vi.stubGlobal("fetch", () => new Promise(() => {}));

    const { result } = await renderHook(() => usePreferences(), { wrapper });

    expect(result.current[0]).toEqual({ showStreamStatistics: false, snapToGrid: true });
  });

  test("shows a change right away and saves all preferences", async () => {
    queryClient.setQueryData(getGetPreferencesQueryKey(), stored);
    savePreferences.mockResolvedValue({ ...stored, showStreamStatistics: true });

    const { result, act } = await renderHook(() => usePreferences(), { wrapper });
    await act(() => result.current[1]({ showStreamStatistics: true }));

    expect(result.current[0]).toEqual({ ...stored, showStreamStatistics: true });
    expect(savePreferences).toHaveBeenCalledWith({ ...stored, showStreamStatistics: true });
  });

  test("reloads the preferences when saving failed", async () => {
    queryClient.setQueryData(getGetPreferencesQueryKey(), stored);
    savePreferences.mockRejectedValue(new Error("Engine gone"));
    const invalidate = vi.spyOn(queryClient, "invalidateQueries").mockResolvedValue();

    const { result, act } = await renderHook(() => usePreferences(), { wrapper });
    await act(() => result.current[1]({ snapToGrid: false }));

    await expect.poll(() => invalidate).toHaveBeenCalledWith({ queryKey: getGetPreferencesQueryKey() });
  });
});

describe("useModuleData", () => {
  test("returns the module's latest data and ends the subscription when the module changes", async () => {
    const listeners = new Map<string, (data: unknown) => void>();
    const unsubscribe = vi.fn();
    vi.spyOn(connection, "subscribe").mockImplementation((moduleId, listener) => {
      listeners.set(moduleId, listener);
      return () => unsubscribe(moduleId);
    });

    const { result, rerender, act } = await renderHook((moduleId?: string) => useModuleData(moduleId!), {
      wrapper,
      initialProps: "spectrum-1",
    });
    expect(result.current).toBeUndefined();

    await act(() => listeners.get("spectrum-1")!({ peak: 1 }));
    expect(result.current).toEqual({ peak: 1 });

    await rerender("spectrum-2");
    expect(unsubscribe).toHaveBeenCalledWith("spectrum-1");
    // not the previous module's data
    expect(result.current).toBeUndefined();
  });
});

describe("useModuleLevels", () => {
  test("returns the levels of the module while mounted", async () => {
    const levels: ModuleLevels = { "gain-1": [{ port: "Output", peak: [0.5], rms: [0.2] }] };
    vi.spyOn(connection, "levels", "get").mockReturnValue(levels);
    const unsubscribe = vi.fn();
    vi.spyOn(connection, "subscribeLevels").mockReturnValue(unsubscribe);

    const { result, unmount } = await renderHook((moduleId?: string) => useModuleLevels(moduleId!), {
      wrapper,
      initialProps: "gain-1",
    });
    expect(result.current).toBe(levels["gain-1"]);

    await unmount();
    expect(unsubscribe).toHaveBeenCalled();
  });

  test("is undefined while the module isn't processed", async () => {
    vi.spyOn(connection, "levels", "get").mockReturnValue({});

    const { result } = await renderHook(() => useModuleLevels("gain-1"), { wrapper });

    expect(result.current).toBeUndefined();
  });
});

test("useModuleUpdate goes through the connection", async () => {
  const update = vi.spyOn(connection, "updateModule").mockImplementation(() => {});
  const module = testModule("Gain", { gain: 1 });

  const { result } = await renderHook(() => useModuleUpdate(), { wrapper });
  result.current(module);

  expect(update).toHaveBeenCalledWith(module);
});
