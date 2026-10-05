import type {
  ConnectionDto,
  CreateSubgraphRequest,
  ModuleDto,
  ModulePosition,
  ModuleTypeDto,
  SubgraphDto,
  SubgraphTemplateDto,
  UiPreferencesDto,
} from "@micser/web-sdk";
import { test as base, expect, type APIRequestContext, type Locator, type Page } from "@playwright/test";
import { engineUrl } from "./environment";

export { expect };

const defaultPreferences: UiPreferencesDto = { showStreamStatistics: false, snapToGrid: true };

/**
 * The engine's API, for setting up and checking what the UI did.
 */
export class EngineApi {
  private readonly request: APIRequestContext;

  constructor(request: APIRequestContext) {
    this.request = request;
  }

  addModule(type: string, position: ModulePosition, properties: { name?: string; subgraphId?: string } = {}) {
    return this.send<ModuleDto>("post", "/api/modules", { type, position, ...properties });
  }

  addSubgraph(request: Omit<CreateSubgraphRequest, "color" | "name"> & Partial<CreateSubgraphRequest>) {
    return this.send<SubgraphDto>("post", "/api/subgraphs", { name: null, color: "Blue", ...request });
  }

  connect(source: ModuleDto, target: ModuleDto) {
    return this.send<ConnectionDto>("post", "/api/connections", {
      sourceModuleId: source.id,
      sourcePort: "Output",
      targetModuleId: target.id,
      targetPort: "Input",
    });
  }

  connections() {
    return this.send<ConnectionDto[]>("get", "/api/connections");
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

  preferences() {
    return this.send<UiPreferencesDto>("get", "/api/preferences");
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

  templates() {
    return this.send<SubgraphTemplateDto[]>("get", "/api/subgraph-templates");
  }

  updateModule(module: ModuleDto) {
    return this.send<ModuleDto>("put", `/api/modules/${module.id}`, module);
  }

  private async send<T>(method: "get" | "post" | "put" | "delete", path: string, data?: unknown): Promise<T> {
    const response = await this.request.fetch(engineUrl + path, { method, data });
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
    await expect(this.page.getByText("Connected", { exact: true })).toBeVisible();
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

export const test = base.extend<{ engine: EngineApi; graph: Graph }>({
  engine: async ({ request }, use) => {
    const engine = new EngineApi(request);
    await engine.reset();
    await use(engine);
  },
  graph: async ({ page, engine }, use) => {
    // the engine is reset before the page opens
    void engine;
    await use(new Graph(page));
  },
});
