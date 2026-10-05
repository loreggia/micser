import { useGetPreferences } from "@micser/web-sdk";
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

export interface WebView {
  postMessage(message: unknown): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
}

type ShellMessage = ({ type: "state" } & ShellState) | { type: "updateCheck"; result: UpdateCheckResult };

/**
 * The desktop shell, when the UI runs in its WebView2 window. Messages go both ways through WebView2 web messages.
 */
export class Shell {
  private readonly listeners = new Set<() => void>();
  private readonly pendingChecks: ((result: UpdateCheckResult) => void)[] = [];
  private readonly webView: WebView;
  private current?: ShellState;

  constructor(webView: WebView) {
    this.webView = webView;
    webView.addEventListener("message", (event) => this.onMessage(event.data as ShellMessage));
    webView.postMessage({ type: "getState" });
  }

  get state() {
    return this.current;
  }

  /** Downloads a newer release, if there is one. */
  checkForUpdates() {
    return new Promise<UpdateCheckResult>((resolve) => {
      this.pendingChecks.push(resolve);
      this.webView.postMessage({ type: "checkForUpdates" });
    });
  }

  /** Stops the engine and restarts Micser into the downloaded update. */
  installUpdate() {
    this.webView.postMessage({ type: "installUpdate" });
  }

  /** Stops the engine process; the shell starts a new one, and the window reloads with it. */
  restartEngine() {
    this.webView.postMessage({ type: "restartEngine" });
  }

  /**
   * Driver changes run elevated (a UAC prompt) while the engine is stopped, so the window reloads afterwards. The shell reports a
   * failure or a needed reboot itself.
   */
  installDriver(cableCount: number) {
    this.webView.postMessage({ type: "installDriver", cableCount });
  }

  setCableCount(cableCount: number) {
    this.webView.postMessage({ type: "setCableCount", cableCount });
  }

  /** The language preference, for the tray menu; null follows the system. */
  setLanguage(language: string | null) {
    this.webView.postMessage({ type: "setLanguage", language });
  }

  /** Changes a cable's layout (cable 1 is the first); like the other driver changes, it restarts the cables. */
  setCableLayout(cable: number, layout: CableLayout) {
    this.webView.postMessage({ type: "setCableLayout", cable, layout });
  }

  updateDriver() {
    this.webView.postMessage({ type: "updateDriver" });
  }

  uninstallDriver() {
    this.webView.postMessage({ type: "uninstallDriver" });
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
    }
  }
}

const webView = (window as { chrome?: { webview?: WebView } }).chrome?.webview;

/** The desktop shell, or undefined in a browser (e.g. the Vite dev server). */
export const shell = webView ? new Shell(webView) : undefined;

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

/** The shell's state, or undefined in a browser or until the shell answered. */
export function useShellState(): ShellState | undefined {
  return useSyncExternalStore(
    (onChange) => shell?.subscribe(onChange) ?? (() => {}),
    () => shell?.state
  );
}
