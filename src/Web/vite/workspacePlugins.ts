import { existsSync, readdirSync, readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { Plugin } from "vite";

export interface WorkspacePlugin {
  /** The URL the engine reports for the plugin's widget bundle, e.g. `/plugins/Main/web/index.js`. */
  url: string;
  /** The widget package's source entry. */
  entry: string;
}

/**
 * The plugins in this repository (`src/Plugins/<name>/plugin.json` with a `Web` package).
 */
export function findWorkspacePlugins(pluginsDirectory: string): WorkspacePlugin[] {
  if (!existsSync(pluginsDirectory)) {
    return [];
  }

  return readdirSync(pluginsDirectory, { withFileTypes: true }).flatMap((folder) => {
    const directory = resolve(pluginsDirectory, folder.name);
    const manifestPath = resolve(directory, "plugin.json");
    const packagePath = resolve(directory, "Web", "package.json");
    if (!folder.isDirectory() || !existsSync(manifestPath) || !existsSync(packagePath)) {
      return [];
    }

    const manifest = JSON.parse(readFileSync(manifestPath, "utf8")) as { id: string; web?: string };
    const webPackage = JSON.parse(readFileSync(packagePath, "utf8")) as { exports: { ".": string } };
    return manifest.web
      ? [
          {
            url: `/plugins/${manifest.id}/${manifest.web.replace(/^\//, "")}`,
            entry: resolve(directory, "Web", webPackage.exports["."]),
          },
        ]
      : [];
  });
}

/**
 * In development, serves the widget bundles of the repository's plugins from their source, so they get hot reloading and share the dev
 * server's modules. Other plugins' bundles come from the engine (see the proxy in vite.config.ts).
 */
export function workspacePluginsPlugin(pluginsDirectory: string): Plugin {
  return {
    name: "micser-workspace-plugins",
    apply: "serve",
    configureServer(server) {
      const plugins = findWorkspacePlugins(pluginsDirectory);
      server.middlewares.use((request, _, next) => {
        const path = request.url?.split("?")[0];
        const plugin = plugins.find((p) => p.url.toLowerCase() === path?.toLowerCase());
        if (plugin) {
          request.url = "/@fs/" + plugin.entry.replace(/\\/g, "/").replace(/^\//, "");
        }

        next();
      });
    },
  };
}
