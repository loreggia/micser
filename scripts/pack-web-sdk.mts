#!/usr/bin/env node
/**
 * Completes the published package of @micser/web-sdk in src/WebSdk/dist, after the Vite build and the declarations
 * (`npm run build:package -w @micser/web-sdk -- <version>`): writes its package.json, which points the entries at the built files,
 * and adds the license and the readme. Publish it with `npm publish src/WebSdk/dist`.
 *
 * Usage: node scripts/pack-web-sdk.mts <version>
 */
import { copyFileSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import process from "node:process";

const version = process.argv[2];
if (!version || !/^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$/.test(version)) {
  console.error("Usage: node scripts/pack-web-sdk.mts <version>");
  process.exit(1);
}

const repoRoot = path.resolve(import.meta.dirname, "..");
const sdkDir = path.join(repoRoot, "src/WebSdk");
const distDir = path.join(sdkDir, "dist");

const source = JSON.parse(readFileSync(path.join(sdkDir, "package.json"), "utf8")) as {
  name: string;
  peerDependencies: Record<string, string>;
  devDependencies: Record<string, string>;
};

const entry = (name: string, types: string) => ({ types: `./types/${types}.d.ts`, default: `./${name}.js` });

const manifest = {
  name: source.name,
  version,
  description:
    "The SDK for the widgets of Micser plugins: plugin definition, engine API client and hooks, controls, and the Vite build.",
  keywords: ["micser", "audio", "plugin", "sdk"],
  license: "Apache-2.0",
  homepage: "https://github.com/loreggia/micser",
  repository: { type: "git", url: "git+https://github.com/loreggia/micser.git", directory: "src/WebSdk" },
  type: "module",
  exports: {
    ".": entry("index", "src/index"),
    "./testing": entry("testing", "testing/index"),
    "./vite": entry("vite", "vite/index"),
  },
  peerDependencies: { ...source.peerDependencies, vite: source.devDependencies.vite },
  peerDependenciesMeta: { vite: { optional: true } },
  publishConfig: { access: "public" },
};

writeFileSync(path.join(distDir, "package.json"), JSON.stringify(manifest, null, 2) + "\n");
copyFileSync(path.join(repoRoot, "LICENSE"), path.join(distDir, "LICENSE"));
copyFileSync(path.join(sdkDir, "README.md"), path.join(distDir, "README.md"));
console.log(`${source.name}@${version} in ${path.relative(repoRoot, distDir)}`);
