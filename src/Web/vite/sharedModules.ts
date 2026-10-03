import { sharedModules } from "@micser/web-sdk/vite";
import { createRequire } from "node:module";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import type { Plugin } from "vite";

const prefix = "virtual:micser-shared/";
const resolvedPrefix = "\0" + prefix;

function entryName(specifier: string) {
  return "shared-" + specifier.replace(/^@/, "").replace(/[^A-Za-z0-9]+/g, "-");
}

/**
 * The source of the module that re-exports a shared module. CommonJS-only packages (React) need their exports listed, since `export *` only
 * re-exports ES modules; packages with an ES module build are re-exported as they are.
 */
function shimSource(specifier: string, root: string) {
  const require = createRequire(resolve(root, "package.json"));
  try {
    const isCommonJsOnly = require.resolve(specifier) === fileURLToPath(import.meta.resolve(specifier));
    const exports: unknown = isCommonJsOnly ? require(specifier) : undefined;
    if (exports && typeof exports === "object") {
      const names = Object.keys(exports).filter((name) => /^[A-Za-z_$][\w$]*$/.test(name) && name !== "default");
      return `export { ${names.join(", ")} } from "${specifier}";\nexport { default } from "${specifier}";\n`;
    }
  } catch {
    // TypeScript source (the web SDK), which Node can't load
  }

  return `export * from "${specifier}";\n`;
}

/**
 * Makes the shared modules (see `sharedModules` in `@micser/web-sdk/vite`) available to plugin bundles: each one becomes an entry chunk that
 * re-exports the UI's instance, and an import map in index.html maps the module names to these chunks. In development, the import map points
 * to modules served by Vite.
 */
export function sharedModulesPlugin(): Plugin {
  let root = process.cwd();
  let base = "/";

  return {
    name: "micser-shared-modules",
    config(config, env) {
      if (env.command !== "build") {
        return;
      }

      const configRoot = resolve(config.root ?? process.cwd());
      return {
        build: {
          rolldownOptions: {
            input: {
              index: resolve(configRoot, "index.html"),
              ...Object.fromEntries(sharedModules.map((specifier) => [entryName(specifier), prefix + specifier])),
            },
            // keeps the exports of the shared entries
            preserveEntrySignatures: "exports-only",
          },
        },
      };
    },
    configResolved(config) {
      root = config.root;
      base = config.base;
    },
    resolveId(id) {
      return id.startsWith(prefix) ? "\0" + id : undefined;
    },
    load(id) {
      return id.startsWith(resolvedPrefix) ? shimSource(id.slice(resolvedPrefix.length), root) : undefined;
    },
    transformIndexHtml: {
      order: "post",
      handler(_, context) {
        const imports: Record<string, string> = {};
        for (const specifier of sharedModules) {
          if (context.bundle) {
            const chunk = Object.values(context.bundle).find(
              (output) => output.type === "chunk" && output.facadeModuleId === resolvedPrefix + specifier
            );
            if (!chunk) {
              throw new Error(`The shared module ${specifier} has no chunk.`);
            }

            imports[specifier] = base + chunk.fileName;
          } else {
            imports[specifier] = `${base}@id/__x00__${prefix}${specifier}`;
          }
        }

        return [
          {
            tag: "script",
            attrs: { type: "importmap" },
            children: JSON.stringify({ imports }),
            injectTo: "head-prepend",
          },
        ];
      },
    },
  };
}
