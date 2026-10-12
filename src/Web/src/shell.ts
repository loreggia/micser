import { useGetPreferences, type UiColorMode } from "@micser/web-sdk";
import { useEffect, useSyncExternalStore } from "react";

/** A cable's channel layout; both sides of the cable use it. */
export type CableLayout = "stereo" | "5.1" | "7.1";

export interface CableStatus {
  layout: CableLayout;
  /** Whether Windows has set the cable's endpoints to the layout's format (the shell fixes them when it reads the status). */
  formatsMatch: boolean;
}

/** The virtual audio cable driver, as Micser.DriverUtility reports it. */
export interface DriverStatus {
  installed: boolean;
  /** The device's problem code, or null when it runs. */
  problem: number | null;
  installedVersion: string | null;
  /** The version of the driver that comes with Micser. */
  bundledVersion: string | null;
  cableCount: number;
  updateAvailable: boolean;
  /** The cables in order (cable 1 first); empty while the driver isn't installed. */
  cables: CableStatus[];
}

export interface DriverState {
  /** A change is running (the engine is stopped meanwhile). */
  isBusy: boolean;
  /** Null until the shell read it, or if reading failed. */
  status: DriverStatus | null;
}

/** What the desktop shell reports about itself (see the shell's MainForm.PostState). */
export interface ShellState {
  /** The installed version, or null in development. */
  version: string | null;
  canUpdate: boolean;
  isCheckingForUpdates: boolean;
  /** A downloaded update that is ready to install, or null. */
  pendingUpdate: string | null;
  /** Whether the shell can restart the engine process (it can't in development). */
  canRestartEngine: boolean;
  /** Null if this copy of Micser has no driver package (development, or no signed driver yet). */
  driver: DriverState | null;
}

export type UpdateCheckResult = "upToDate" | "updateReady" | "failed";

/**
 * How the UI and the shell exchange messages. In the shell's web view (Avalonia's, WebView2 or WebKitGTK), messages go to the shell
 * through `invokeCSharpAction(json)`, and the shell calls `window.micserShell.receive(message)`.
 */
export interface ShellTransport {
  send(message: unknown): void;
  /** Calls the listener with each message from the shell. */
  onReceive(listener: (message: unknown) => void): void;
}

/** The functions the shell's web view provides and calls. */
export interface ShellWindow {
  invokeCSharpAction?: (body: string) => void;
  micserShell?: { receive(message: unknown): void };
}

type ShellMessage =
  | ({ type: "state" } & ShellState)
  | { type: "updateCheck"; result: UpdateCheckResult }
  | { type: "releaseNotes"; version: string; notes: string | null };

/**
 * The desktop shell, when the UI runs in its window. Messages go both ways through a {@link ShellTransport}.
 */
export class Shell {
  private readonly listeners = new Set<() => void>();
  private readonly pendingChecks: ((result: UpdateCheckResult) => void)[] = [];
  private readonly pendingReleaseNotes = new Map<string, ((notes: string | null) => void)[]>();
  private readonly transport: ShellTransport;
  private current?: ShellState;

  constructor(transport: ShellTransport) {
    this.transport = transport;
    transport.onReceive((message) => this.onMessage(message as ShellMessage));
    transport.send({ type: "getState" });
  }

  get state() {
    return this.current;
  }

  /** Downloads a newer release, if there is one. */
  checkForUpdates() {
    return new Promise<UpdateCheckResult>((resolve) => {
      this.pendingChecks.push(resolve);
      this.transport.send({ type: "checkForUpdates" });
    });
  }

  /**
   * The markdown release notes of the installed version or of the downloaded update, or null for another version or a release
   * without notes.
   */
  getReleaseNotes(version: string) {
    return new Promise<string | null>((resolve) => {
      const waiting = this.pendingReleaseNotes.get(version);
      if (waiting) {
        waiting.push(resolve);
      } else {
        this.pendingReleaseNotes.set(version, [resolve]);
        this.transport.send({ type: "getReleaseNotes", version });
      }
    });
  }

  /** Stops the engine and restarts Micser into the downloaded update. */
  installUpdate() {
    this.transport.send({ type: "installUpdate" });
  }

  /** Stops the engine process; the shell starts a new one, and the window reloads with it. */
  restartEngine() {
    this.transport.send({ type: "restartEngine" });
  }

  /**
   * Driver changes run elevated (a UAC prompt) while the engine is stopped, so the window reloads afterwards. The shell reports a
   * failure or a needed reboot itself.
   */
  installDriver(cableCount: number) {
    this.transport.send({ type: "installDriver", cableCount });
  }

  setCableCount(cableCount: number) {
    this.transport.send({ type: "setCableCount", cableCount });
  }

  /** The language preference, for the tray menu; null follows the system. */
  setLanguage(language: string | null) {
    this.transport.send({ type: "setLanguage", language });
  }

  /** The theme preference's color mode, for the window's title bar and background; null follows the system. */
  setColorMode(colorMode: UiColorMode | null) {
    this.transport.send({ type: "setColorMode", colorMode });
  }

  /** Changes a cable's layout (cable 1 is the first); like the other driver changes, it restarts the cables. */
  setCableLayout(cable: number, layout: CableLayout) {
    this.transport.send({ type: "setCableLayout", cable, layout });
  }

  updateDriver() {
    this.transport.send({ type: "updateDriver" });
  }

  uninstallDriver() {
    this.transport.send({ type: "uninstallDriver" });
  }

  subscribe(listener: () => void) {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  private onMessage(message: ShellMessage) {
    if (message.type === "state") {
      this.current = {
        version: message.version,
        canUpdate: message.canUpdate,
        isCheckingForUpdates: message.isCheckingForUpdates,
        pendingUpdate: message.pendingUpdate,
        canRestartEngine: message.canRestartEngine,
        driver: message.driver,
      };
      this.listeners.forEach((listener) => listener());
    } else if (message.type === "updateCheck") {
      this.pendingChecks.splice(0).forEach((resolve) => resolve(message.result));
    } else if (message.type === "releaseNotes") {
      this.pendingReleaseNotes.get(message.version)?.forEach((resolve) => resolve(message.notes));
      this.pendingReleaseNotes.delete(message.version);
    }
  }
}

/** The shell's transport, if the UI runs in its web view, which provides `invokeCSharpAction` before the UI loads. */
export function createTransport(target: ShellWindow): ShellTransport | undefined {
  if (!target.invokeCSharpAction) {
    return undefined;
  }

  const listeners: ((message: unknown) => void)[] = [];
  target.micserShell = { receive: (message) => listeners.forEach((listener) => listener(message)) };
  return {
    send: (message) => target.invokeCSharpAction?.(JSON.stringify(message)),
    onReceive: (listener) => listeners.push(listener),
  };
}

const transport = createTransport(window as ShellWindow);

/** The desktop shell, or undefined in a browser (e.g. the Vite dev server). */
export const shell = transport ? new Shell(transport) : undefined;

if (shell) {
  // like an app's window, the shell has no browser context menu (WebView2 turns it off, WebKitGTK can't)
  document.addEventListener("contextmenu", (event) => event.preventDefault());
}

/** Passes the language preference to the shell once it's loaded and whenever it changes. */
export function useShellLanguage() {
  const { data: preferences } = useGetPreferences();
  const language = preferences ? (preferences.language ?? null) : undefined;

  useEffect(() => {
    if (language !== undefined) {
      shell?.setLanguage(language);
    }
  }, [language]);
}

/** Passes the theme preference's color mode to the shell once it's loaded and whenever it changes. */
export function useShellColorMode() {
  const { data: preferences } = useGetPreferences();
  const colorMode = preferences ? (preferences.theme?.colorMode ?? null) : undefined;

  useEffect(() => {
    if (colorMode !== undefined) {
      shell?.setColorMode(colorMode);
    }
  }, [colorMode]);
}

/** The shell's state, or undefined in a browser or until the shell answered. */
export function useShellState(): ShellState | undefined {
  return useSyncExternalStore(
    (onChange) => shell?.subscribe(onChange) ?? (() => {}),
    () => shell?.state
  );
}
