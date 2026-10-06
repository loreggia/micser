import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import type { Connect, ViteDevServer } from "vite";
import { afterAll, beforeAll, describe, expect, test } from "vitest";
import { findWorkspacePlugins, workspacePluginsPlugin } from "../workspacePlugins";

let pluginsDirectory: string;

function writeJson(path: string, value: unknown) {
  mkdirSync(join(path, ".."), { recursive: true });
  writeFileSync(path, JSON.stringify(value));
}

beforeAll(() => {
  pluginsDirectory = mkdtempSync(join(tmpdir(), "micser-plugins-"));

  writeJson(join(pluginsDirectory, "Main", "plugin.json"), { id: "Main", web: "/web/index.js" });
  writeJson(join(pluginsDirectory, "Main", "Web", "package.json"), { exports: { ".": "./src/index.ts" } });
  // no widgets
  writeJson(join(pluginsDirectory, "Headless", "plugin.json"), { id: "Headless" });
  writeJson(join(pluginsDirectory, "Headless", "Web", "package.json"), { exports: { ".": "./src/index.ts" } });
  // no widget package
  writeJson(join(pluginsDirectory, "DotnetOnly", "plugin.json"), { id: "DotnetOnly", web: "web/index.js" });
  writeFileSync(join(pluginsDirectory, "README.md"), "");
});

afterAll(() => {
  rmSync(pluginsDirectory, { recursive: true, force: true });
});

describe("findWorkspacePlugins", () => {
  test("finds the plugins with a widget package and a web entry", () => {
    expect(findWorkspacePlugins(pluginsDirectory)).toEqual([
      { url: "/plugins/Main/web/index.js", entry: join(pluginsDirectory, "Main", "Web", "src", "index.ts") },
    ]);
  });

  test("finds nothing without the folder", () => {
    expect(findWorkspacePlugins(join(pluginsDirectory, "missing"))).toEqual([]);
  });
});

describe("workspacePluginsPlugin", () => {
  function serve() {
    let middleware: Connect.NextHandleFunction | undefined;
    const server = {
      middlewares: { use: (handler: Connect.NextHandleFunction) => (middleware = handler) },
    } as unknown as ViteDevServer;

    const plugin = workspacePluginsPlugin(pluginsDirectory);
    (plugin.configureServer as (server: ViteDevServer) => void)(server);

    return (url: string) => {
      const request = { url } as Connect.IncomingMessage;
      let calledNext = false;
      middleware!(request, {} as never, () => (calledNext = true));
      expect(calledNext).toBe(true);
      return request.url;
    };
  }

  test("only runs in the dev server", () => {
    expect(workspacePluginsPlugin(pluginsDirectory).apply).toBe("serve");
  });

  test("serves a workspace plugin's bundle from its source, ignoring case and query", () => {
    const request = serve();
    const entry = join(pluginsDirectory, "Main", "Web", "src", "index.ts").replace(/\\/g, "/").replace(/^\//, "");

    expect(request("/plugins/main/WEB/index.js?t=1")).toBe(`/@fs/${entry}`);
  });

  test("leaves other requests to the proxy", () => {
    const request = serve();

    expect(request("/plugins/Other/web/index.js")).toBe("/plugins/Other/web/index.js");
    expect(request("/api/plugins")).toBe("/api/plugins");
  });
});
