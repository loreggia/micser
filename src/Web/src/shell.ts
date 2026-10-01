import { useSyncExternalStore } from "react";

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
}

export type UpdateCheckResult = "upToDate" | "updateReady" | "failed";

interface WebView {
  postMessage(message: unknown): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
}

type ShellMessage = ({ type: "state" } & ShellState) | { type: "updateCheck"; result: UpdateCheckResult };

/**
 * The desktop shell, when the UI runs in its WebView2 window. Messages go both ways through WebView2 web messages.
 */
class Shell {
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

/** The shell's state, or undefined in a browser or until the shell answered. */
export function useShellState(): ShellState | undefined {
  return useSyncExternalStore(
    (onChange) => shell?.subscribe(onChange) ?? (() => {}),
    () => shell?.state
  );
}
