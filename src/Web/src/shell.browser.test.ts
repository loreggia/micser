import { expect, test, vi } from "vitest";
import { renderHook } from "vitest-browser-react";
import { Shell, shell, useShellState, type ShellState, type WebView } from "./shell";

const state: ShellState = {
  version: "1.2.0",
  canUpdate: true,
  isCheckingForUpdates: false,
  pendingUpdate: null,
  canRestartEngine: true,
  driver: null,
};

function createWebView() {
  let listener: ((event: MessageEvent) => void) | undefined;
  const webView = {
    postMessage: vi.fn<(message: unknown) => void>(),
    addEventListener: (_: "message", added: (event: MessageEvent) => void) => {
      listener = added;
    },
  } satisfies WebView;

  const receive = (message: unknown) => listener?.(new MessageEvent("message", { data: message }));
  return { webView, receive };
}

test("asks the shell for its state", () => {
  const { webView } = createWebView();

  new Shell(webView);

  expect(webView.postMessage).toHaveBeenCalledWith({ type: "getState" });
});

test("keeps the reported state and tells the subscribers", () => {
  const { webView, receive } = createWebView();
  const shell = new Shell(webView);
  const listener = vi.fn();
  shell.subscribe(listener);

  receive({ type: "state", ...state, unknownField: 1 });

  expect(shell.state).toEqual(state);
  expect(listener).toHaveBeenCalledOnce();
});

test("stops telling a subscriber after it unsubscribed", () => {
  const { webView, receive } = createWebView();
  const shell = new Shell(webView);
  const listener = vi.fn();
  shell.subscribe(listener)();

  receive({ type: "state", ...state });

  expect(listener).not.toHaveBeenCalled();
});

test("an update check resolves all waiting checks with the shell's result", async () => {
  const { webView, receive } = createWebView();
  const shell = new Shell(webView);

  const first = shell.checkForUpdates();
  const second = shell.checkForUpdates();
  expect(webView.postMessage).toHaveBeenLastCalledWith({ type: "checkForUpdates" });

  receive({ type: "updateCheck", result: "updateReady" });

  await expect(first).resolves.toBe("updateReady");
  await expect(second).resolves.toBe("updateReady");
});

test("sends the commands with their arguments", () => {
  const { webView } = createWebView();
  const shell = new Shell(webView);

  shell.installUpdate();
  shell.restartEngine();
  shell.installDriver(2);
  shell.setCableCount(3);
  shell.setCableLayout(1, "5.1");
  shell.updateDriver();
  shell.uninstallDriver();

  expect(webView.postMessage.mock.calls.slice(1).map(([message]) => message)).toEqual([
    { type: "installUpdate" },
    { type: "restartEngine" },
    { type: "installDriver", cableCount: 2 },
    { type: "setCableCount", cableCount: 3 },
    { type: "setCableLayout", cable: 1, layout: "5.1" },
    { type: "updateDriver" },
    { type: "uninstallDriver" },
  ]);
});

test("there is no shell in a plain browser", async () => {
  const { result } = await renderHook(() => useShellState());

  expect(shell).toBeUndefined();
  expect(result.current).toBeUndefined();
});
