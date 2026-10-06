import { sharedModules } from "@micser/web-sdk/vite";
import { resolve } from "node:path";
import type { IndexHtmlTransformContext, IndexHtmlTransformHook, ResolvedConfig, UserConfig } from "vite";
import { describe, expect, test } from "vitest";
import { sharedModulesPlugin } from "../sharedModules";

const webRoot = resolve(import.meta.dirname, "../..");

function createPlugin(base = "/") {
  const plugin = sharedModulesPlugin();
  (plugin.configResolved as (config: ResolvedConfig) => void)({ root: webRoot, base } as ResolvedConfig);

  const load = (specifier: string) =>
    (plugin.load as (id: string) => string | undefined)("\0virtual:micser-shared/" + specifier);
  const transformIndexHtml = (context: Partial<IndexHtmlTransformContext>) => {
    const hook = plugin.transformIndexHtml as { handler: IndexHtmlTransformHook };
    return hook.handler.call(undefined as never, "", context as IndexHtmlTransformContext) as {
      children: string;
    }[];
  };

  return { plugin, load, transformIndexHtml };
}

function importMap(tags: { children: string }[]) {
  return (JSON.parse(tags[0].children) as { imports: Record<string, string> }).imports;
}

describe("sharedModulesPlugin", () => {
  test("adds an entry per shared module to the build only", () => {
    const { plugin } = createPlugin();
    const config = plugin.config as (config: UserConfig, env: { command: string }) => UserConfig | undefined;

    expect(config({ root: webRoot }, { command: "serve" })).toBeUndefined();

    const input = config({ root: webRoot }, { command: "build" })?.build?.rolldownOptions?.input as Record<
      string,
      string
    >;
    expect(input).toMatchObject({
      index: resolve(webRoot, "index.html"),
      "shared-react": "virtual:micser-shared/react",
      "shared-react-jsx-runtime": "virtual:micser-shared/react/jsx-runtime",
      "shared-micser-web-sdk": "virtual:micser-shared/@micser/web-sdk",
    });
    expect(Object.keys(input)).toHaveLength(sharedModules.length + 1);
  });

  test("resolves only its virtual modules", () => {
    const { plugin } = createPlugin();
    const resolveId = plugin.resolveId as (id: string) => string | undefined;

    expect(resolveId("virtual:micser-shared/react")).toBe("\0virtual:micser-shared/react");
    expect(resolveId("react")).toBeUndefined();
  });

  test("lists the exports of CommonJS packages", () => {
    const source = createPlugin().load("react")!;

    expect(source).toMatch(/^export \{ .*\buseState\b.* \} from "react";/);
    expect(source).toContain('export { default } from "react";');
  });

  test("re-exports ES modules and TypeScript source as they are", () => {
    const { load } = createPlugin();

    expect(load("@fluentui/react-components")).toBe('export * from "@fluentui/react-components";\n');
    expect(load("@micser/web-sdk")).toBe('export * from "@micser/web-sdk";\n');
  });

  test("maps the shared modules to the dev server's modules in development", () => {
    const imports = importMap(createPlugin("/ui/").transformIndexHtml({}));

    expect(Object.keys(imports)).toEqual(sharedModules);
    expect(imports.react).toBe("/ui/@id/__x00__virtual:micser-shared/react");
  });

  test("maps the shared modules to their chunks in the build", () => {
    const bundle = Object.fromEntries(
      sharedModules.map((specifier, index) => [
        `chunk-${index}`,
        {
          type: "chunk",
          fileName: `assets/shared-${index}.js`,
          facadeModuleId: "\0virtual:micser-shared/" + specifier,
        },
      ])
    );

    const imports = importMap(createPlugin().transformIndexHtml({ bundle: bundle as never }));

    expect(imports.react).toBe("/assets/shared-0.js");
    expect(imports["@micser/web-sdk"]).toBe(`/assets/shared-${sharedModules.length - 1}.js`);
  });

  test("fails the build when a shared module has no chunk", () => {
    const { transformIndexHtml } = createPlugin();

    expect(() => transformIndexHtml({ bundle: {} })).toThrow("The shared module react has no chunk.");
  });
});
