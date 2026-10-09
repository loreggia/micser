import type {
  ConnectionDto,
  CreateSubgraphRequest,
  ModuleDto,
  ModulePosition,
  ModuleTypeDto,
  PluginDto,
  SubgraphDto,
  SubgraphTemplateDto,
  UiPreferencesDto,
} from "@micser/web-sdk";
import { test as base, expect, type APIRequestContext, type Browser, type Locator, type Page } from "@playwright/test";
import type { ShellState } from "../../src/Web/src/shell";
import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { startEngine, startWeb } from "./servers";

export { expect };

const defaultPreferences: UiPreferencesDto = { showStreamStatistics: false, snapToGrid: true, language: null };

/**
 * The engine's API, for setting up and checking what the UI did.
 */
export class EngineApi {
  private readonly request: APIRequestContext;
  private readonly url: string;

  constructor(request: APIRequestContext, url: string) {
    this.request = request;
    this.url = url;
  }

  addModule(type: string, position: ModulePosition, properties: { name?: string; subgraphId?: string } = {}) {
    return this.send<ModuleDto>("post", "/api/modules", { type, position, ...properties });
  }

  addSubgraph(request: Omit<CreateSubgraphRequest, "color" | "name"> & Partial<CreateSubgraphRequest>) {
    return this.send<SubgraphDto>("post", "/api/subgraphs", { name: null, color: "Blue", ...request });
  }

  /** Connects the modules' first ports, or single channels of them. */
  connect(source: ModuleDto, target: ModuleDto, channels: { sourceChannel?: number; targetChannel?: number } = {}) {
    return this.send<ConnectionDto>("post", "/api/connections", {
      sourceModuleId: source.id,
      sourcePort: "Output",
      targetModuleId: target.id,
      targetPort: "Input",
      ...channels,
    });
  }

  connections() {
    return this.send<ConnectionDto[]>("get", "/api/connections");
  }

  disconnect(id: string) {
    return this.send<void>("delete", `/api/connections/${id}`);
  }

  deleteModule(id: string) {
    return this.send<void>("delete", `/api/modules/${id}`);
  }

  async module(id: string) {
    return (await this.modules()).find((module) => module.id === id);
  }

  modules() {
    return this.send<ModuleDto[]>("get", "/api/modules");
  }

  moduleTypes() {
    return this.send<ModuleTypeDto[]>("get", "/api/module-types");
  }

  plugins() {
    return this.send<PluginDto[]>("get", "/api/plugins");
  }

  preferences() {
    return this.send<UiPreferencesDto>("get", "/api/preferences");
  }

  async setPreferences(changes: Partial<UiPreferencesDto>) {
    return this.send<UiPreferencesDto>("put", "/api/preferences", { ...(await this.preferences()), ...changes });
  }

  /** Removes everything the tests create and restores the default preferences. */
  async reset() {
    for (const subgraph of await this.subgraphs()) {
      await this.send("delete", `/api/subgraphs/${subgraph.id}?deleteModules=true`);
    }

    for (const module of await this.modules()) {
      await this.deleteModule(module.id);
    }

    for (const template of await this.templates()) {
      await this.send("delete", `/api/subgraph-templates/${template.id}`);
    }

    await this.send("put", "/api/preferences", defaultPreferences);
  }

  subgraphs() {
    return this.send<SubgraphDto[]>("get", "/api/subgraphs");
  }

  async builtInTemplates() {
    return (await this.send<SubgraphTemplateDto[]>("get", "/api/subgraph-templates")).filter((t) => t.isBuiltIn);
  }

  /** The user's templates, without the built-in ones. */
  async templates() {
    return (await this.send<SubgraphTemplateDto[]>("get", "/api/subgraph-templates")).filter((t) => !t.isBuiltIn);
  }

  updateModule(module: ModuleDto) {
    return this.send<ModuleDto>("put", `/api/modules/${module.id}`, module);
  }

  updateSubgraph(subgraph: SubgraphDto) {
    return this.send<SubgraphDto>("put", `/api/subgraphs/${subgraph.id}`, subgraph);
  }

  private async send<T>(method: "get" | "post" | "put" | "delete", path: string, data?: unknown): Promise<T> {
    const response = await this.request.fetch(this.url + path, { method, data });
    if (!response.ok()) {
      throw new Error(`${method.toUpperCase()} ${path} failed with ${response.status()}: ${await response.text()}`);
    }

    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }
}

/**
 * The graph editor in the page.
 */
export class Graph {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  get pane() {
    return this.page.locator(".react-flow__pane");
  }

  get edges() {
    return this.page.locator(".react-flow__edge");
  }

  /** The menu that's open, e.g. after a right-click. */
  get menu() {
    return this.page.getByRole("menu").last();
  }

  /** A module's or subgraph's node. */
  node(id: string) {
    return this.page.locator(`.react-flow__node[data-id="${id}"]`);
  }

  /** The first input or output of a node. */
  port(node: Locator, type: "input" | "output") {
    return node.locator(`.react-flow__handle.${type === "input" ? "target" : "source"}`).first();
  }

  /** A port's connector by its handle id: the port's name, or "<port>:<channel>" for one of its channels (0-based). */
  handle(node: Locator, handleId: string) {
    return node.locator(`.react-flow__handle[data-handleid="${handleId}"]`);
  }

  /** Opens a module's or subgraph's "More" menu. */
  async openMoreMenu(node: Locator) {
    await node.getByRole("button", { name: "More" }).click();
  }

  /** A notification's title. */
  notification(title: string) {
    return this.page.locator(".fui-ToastTitle").getByText(title, { exact: true });
  }

  /** A module's title, which is clear of the node's buttons and widget. */
  title(node: Locator) {
    return node.locator(".fui-CardHeader__header");
  }

  /** Clicks the connection between two modules halfway, which selects it. */
  async clickConnection(source: Locator, target: Locator) {
    const from = (await this.port(source, "output").boundingBox())!;
    const to = (await this.port(target, "input").boundingBox())!;
    await this.page.mouse.click((from.x + from.width / 2 + to.x + to.width / 2) / 2, (from.y + to.y + from.height) / 2);
  }

  /** Opens the UI and waits until it shows the engine's graph. */
  async open() {
    await this.page.goto("/");
    // "Connected" in the UI's languages
    await expect(this.page.getByText(/^(Connected|Verbunden)$/)).toBeVisible();
    await expect(this.pane).toBeVisible();
  }

  /** Drags a connection from one port to another. */
  async connect(from: Locator, to: Locator) {
    await from.hover();
    await this.page.mouse.down();
    await to.hover();
    await this.page.mouse.up();
  }

  /** Drags a node by its header to a point relative to where it is. */
  async moveBy(node: Locator, dx: number, dy: number) {
    const box = (await node.boundingBox())!;
    // the card's left edge, clear of the title and the buttons
    const start = { x: box.x + 8, y: box.y + 24 };
    await this.page.mouse.move(start.x, start.y);
    await this.page.mouse.down();
    await this.page.mouse.move(start.x + dx / 2, start.y + dy / 2, { steps: 5 });
    await this.page.mouse.move(start.x + dx, start.y + dy, { steps: 5 });
    await this.page.mouse.up();
  }
}

/**
 * The desktop shell, as the UI sees it in WebView2: `chrome.webview`, injected before the UI loads. It records the messages the UI
 * posts and answers `getState` with the state it was installed with.
 */
export class FakeShell {
  private readonly page: Page;

  private constructor(page: Page) {
    this.page = page;
  }

  static async install(page: Page, state: ShellState) {
    await page.addInitScript((initialState) => {
      const listeners: ((event: MessageEvent) => void)[] = [];
      const shell = {
        sent: [] as unknown[],
        receive: (data: unknown) => listeners.forEach((listener) => listener(new MessageEvent("message", { data }))),
      };
      const target = window as unknown as { chrome?: Record<string, unknown>; micserShell: typeof shell };
      target.micserShell = shell;
      target.chrome ??= {};
      target.chrome.webview = {
        postMessage: (message: { type: string }) => {
          shell.sent.push(message);
          if (message.type === "getState") {
            setTimeout(() => shell.receive({ type: "state", ...initialState }));
          }
        },
        addEventListener: (_: string, listener: (event: MessageEvent) => void) => listeners.push(listener),
      };
    }, state);

    return new FakeShell(page);
  }

  /** The messages the UI posted, except the state requests and the language preference, which the UI sends on its own. */
  async messages() {
    return (await this.sent()).filter((message) => message.type !== "getState" && message.type !== "setLanguage");
  }

  /** The language preferences the UI passed to the shell, in order. */
  async languages() {
    return (await this.sent())
      .filter((message) => message.type === "setLanguage")
      .map((message) => message.language as string | null);
  }

  /** Posts a message to the UI, as the shell does. */
  async send(message: { type: string } & Record<string, unknown>) {
    await this.page.evaluate(
      (data) => (window as unknown as { micserShell: { receive(data: unknown): void } }).micserShell.receive(data),
      message
    );
  }

  async setState(state: ShellState) {
    await this.send({ type: "state", ...state });
  }

  private sent() {
    return this.page.evaluate(
      () =>
        (window as unknown as { micserShell: { sent: ({ type: string } & Record<string, unknown>)[] } }).micserShell
          .sent
    );
  }
}

/** A shell state for tests: a development build that can't update, restart the engine, or install the driver. */
export const developmentShell: ShellState = {
  version: null,
  canUpdate: false,
  isCheckingForUpdates: false,
  pendingUpdate: null,
  canRestartEngine: false,
  driver: null,
};

interface Servers {
  engineUrl: string;
  webUrl: string;
  /** Restarts the engine at the same address, as the shell does to apply plugin changes. */
  restartEngine(): Promise<void>;
}

/** Starts an engine and a Vite server for it (unless the engine is published) and loads the UI once, then stops them after use. */
async function withServers(browser: Browser, use: (servers: Servers) => Promise<void>) {
  const directory = mkdtempSync(join(tmpdir(), "micser-e2e-"));
  const engine = await startEngine(process.env.MICSER_E2E_ENGINE!, directory);
  try {
    // a published engine serves the built UI itself
    const web = process.env.MICSER_E2E_PUBLISHED_ENGINE
      ? { url: engine.url, stop: async () => {} }
      : await startWeb(engine.url, join(directory, "vite"));
    try {
      // with Vite, the first load compiles the UI, which takes long while the other workers do the same
      const page = await browser.newPage();
      // this page isn't traced, so its errors go into the failure, e.g. a module that didn't load
      const problems: string[] = [];
      page.on("console", (message) => void (message.type() === "error" && problems.push(message.text())));
      page.on("requestfailed", (request) => void problems.push(`${request.url()}: ${request.failure()?.errorText}`));
      await page.goto(web.url);
      try {
        await expect(page.getByText("Connected", { exact: true })).toBeVisible({ timeout: 60_000 });
      } catch (error) {
        throw new Error(`The UI didn't connect to the engine.\n${problems.join("\n")}`, { cause: error });
      }

      await page.close();

      await use({ engineUrl: engine.url, webUrl: web.url, restartEngine: () => engine.restart() });
    } finally {
      await web.stop();
    }
  } finally {
    await engine.stop();
    rmSync(directory, { recursive: true, force: true, maxRetries: 5 });
  }
}

export const test = base.extend<
  { app: Servers; engine: EngineApi; graph: Graph; ownEngine: boolean },
  { servers: Servers }
>({
  // an engine and a Vite server per worker, so workers don't share the graph
  servers: [async ({ browser }, use) => withServers(browser, use), { scope: "worker", timeout: 120_000 }],
  // for tests that change the engine beyond what EngineApi.reset() undoes, e.g. its plugins
  ownEngine: [false, { option: true }],
  app: [
    async ({ browser, servers, ownEngine }, use) => (ownEngine ? withServers(browser, use) : use(servers)),
    { timeout: 120_000 },
  ],
  baseURL: async ({ app }, use) => {
    await use(app.webUrl);
  },
  engine: async ({ request, app }, use) => {
    const engine = new EngineApi(request, app.engineUrl);
    await engine.reset();
    await use(engine);
  },
  graph: async ({ page, engine }, use) => {
    // the engine is reset before the page opens
    void engine;
    await use(new Graph(page));
  },
});
