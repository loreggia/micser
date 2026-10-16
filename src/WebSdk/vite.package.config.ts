import { readFileSync } from "node:fs";
import { defineConfig } from "vite";

const { peerDependencies } = JSON.parse(readFileSync(new URL("package.json", import.meta.url), "utf8")) as {
  peerDependencies: Record<string, string>;
};
// the peer dependencies and their subpaths (react/jsx-runtime), and vite for the types of the vite entry
const external = new RegExp(`^(${[...Object.keys(peerDependencies), "vite"].join("|")})(/|$)`);

/**
 * The JavaScript of the published package (`npm run build:package`): one ES module per entry, with the code they share in chunks, so the
 * package has one i18next instance and engine context. The repository's workspaces use the TypeScript source instead.
 */
export default defineConfig({
  build: {
    lib: {
      entry: { index: "src/index.ts", testing: "testing/index.tsx", vite: "vite/index.ts" },
      formats: ["es"],
      fileName: (_format, name) => `${name}.js`,
    },
    outDir: "dist",
    emptyOutDir: true,
    minify: false,
    sourcemap: true,
    rolldownOptions: { external },
  },
});
