import type { UserConfig } from "vite";

/**
 * The modules a plugin's widget bundle imports from the web UI instead of bundling them, so that plugins share React, Fluent UI, the query
 * cache and the engine connection with it. The UI maps them to its own chunks with an import map.
 */
export const sharedModules = [
  "react",
  "react/jsx-runtime",
  "@fluentui/react-components",
  "@tanstack/react-query",
  "@micser/web-sdk",
];

/**
 * The Vite configuration of a plugin's widget bundle: an ES module at `dist/index.js` whose default export is the plugin (see
 * `definePlugin`). Everything except the {@link sharedModules} is bundled.
 */
export function definePluginBuild(entry = "src/index.ts"): UserConfig {
  return {
    // bundled dependencies may check it; a library build doesn't replace it
    define: { "process.env.NODE_ENV": JSON.stringify("production") },
    build: {
      lib: { entry, formats: ["es"], fileName: "index" },
      outDir: "dist",
      emptyOutDir: true,
      rolldownOptions: {
        external: sharedModules,
        onLog(level, log, defaultHandler) {
          // "use client" in bundled libraries means nothing outside server components
          if (log.code !== "MODULE_LEVEL_DIRECTIVE") {
            defaultHandler(level, log);
          }
        },
      },
    },
  };
}
