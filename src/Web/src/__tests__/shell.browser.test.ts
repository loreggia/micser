import { expect, test, vi } from "vitest";
import { renderHook } from "vitest-browser-react";
import {
  createTransport,
  Shell,
  shell,
  useShellState,
  type ShellState,
  type ShellTransport,
  type ShellWindow,
} from "../shell";

const state: ShellState = {
  version: "1.2.0",
  canUpdate: true,
  isCheckingForUpdates: false,
  pendingUpdate: null,
  canRestartEngine: true,
  driver: null,
  autostart: true,
  menuEntry: null,
};

function createFakeTransport() {
  let listener: ((message: unknown) => void) | undefined;
  const transport = {
    send: vi.fn<(message: unknown) => void>(),
    onReceive: (added: (message: unknown) => void) => {
      listener = added;
    },
  } satisfies ShellTransport;

  const receive = (message: unknown) => listener?.(message);
  return { transport, receive };
}

test("asks the shell for its state", () => {
  const { transport } = createFakeTransport();

  new Shell(transport);

  expect(transport.send).toHaveBeenCalledWith({ type: "getState" });
});

test("keeps the reported state and tells the subscribers", () => {
  const { transport, receive } = createFakeTransport();
  const shell = new Shell(transport);
  const listener = vi.fn();
  shell.subscribe(listener);

  receive({ type: "state", ...state, unknownField: 1 });

  expect(shell.state).toEqual(state);
  expect(listener).toHaveBeenCalledOnce();
});

test("stops telling a subscriber after it unsubscribed", () => {
  const { transport, receive } = createFakeTransport();
  const shell = new Shell(transport);
  const listener = vi.fn();
  shell.subscribe(listener)();

  receive({ type: "state", ...state });

  expect(listener).not.toHaveBeenCalled();
});

test("an update check resolves all waiting checks with the shell's result", async () => {
  const { transport, receive } = createFakeTransport();
  const shell = new Shell(transport);

  const first = shell.checkForUpdates();
  const second = shell.checkForUpdates();
  expect(transport.send).toHaveBeenLastCalledWith({ type: "checkForUpdates" });

  receive({ type: "updateCheck", result: "updateReady" });

  await expect(first).resolves.toBe("updateReady");
  await expect(second).resolves.toBe("updateReady");
});

test("asks for a version's release notes once and resolves the waiting requests with the answer for that version", async () => {
  const { transport, receive } = createFakeTransport();
  const shell = new Shell(transport);

  const first = shell.getReleaseNotes("2.0.0");
  const second = shell.getReleaseNotes("2.0.0");
  const other = shell.getReleaseNotes("1.2.0");
  expect(transport.send.mock.calls.slice(1).map(([message]) => message)).toEqual([
    { type: "getReleaseNotes", version: "2.0.0" },
    { type: "getReleaseNotes", version: "1.2.0" },
  ]);

  receive({ type: "releaseNotes", version: "2.0.0", notes: "- New" });
  receive({ type: "releaseNotes", version: "1.2.0", notes: null });

  await expect(first).resolves.toBe("- New");
  await expect(second).resolves.toBe("- New");
  await expect(other).resolves.toBeNull();
});

test("sends the commands with their arguments", () => {
  const { transport } = createFakeTransport();
  const shell = new Shell(transport);

  shell.installUpdate();
  shell.restartEngine();
  shell.installDriver(2);
  shell.setCableCount(3);
  shell.setCableLayout(1, "5.1");
  shell.updateDriver();
  shell.uninstallDriver();

  expect(transport.send.mock.calls.slice(1).map(([message]) => message)).toEqual([
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

test("talks to the shell's web view through invokeCSharpAction and micserShell.receive", () => {
  const target: ShellWindow = { invokeCSharpAction: vi.fn<(body: string) => void>() };
  const transport = createTransport(target)!;
  const listener = vi.fn();
  transport.onReceive(listener);

  transport.send({ type: "getState" });
  target.micserShell!.receive({ type: "state" });

  expect(target.invokeCSharpAction).toHaveBeenCalledWith('{"type":"getState"}');
  expect(listener).toHaveBeenCalledWith({ type: "state" });
});

test("has no transport without the shell's web view", () => {
  expect(createTransport({})).toBeUndefined();
});
